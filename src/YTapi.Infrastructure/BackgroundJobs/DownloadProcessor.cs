using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YTapi.Application.DTOs.Responses;
using YTapi.Application.Interfaces;
using YTapi.Domain.Common;
using YTapi.Domain.Entities;
using YTapi.Domain.Enums;
using YTapi.Domain.ValueObjects;
using YTapi.Infrastructure.Configuration;

namespace YTapi.Infrastructure.BackgroundJobs;

/// <summary>
/// Processes download jobs by downloading audio from YouTube and converting to MP3.
/// Handles both single track and multi-track (album) downloads.
/// </summary>
public sealed class DownloadProcessor : IDownloadProcessor
{
    private readonly IYoutubeAudioDownloader _youtubeDownloader;
    private readonly IDownloadJobStore _jobStore;
    private readonly IProgressNotifier _progressNotifier;
    private readonly ILogger<DownloadProcessor> _logger;
    private readonly DownloadSettings _downloadSettings;

    public DownloadProcessor(
        IYoutubeAudioDownloader youtubeDownloader,
        IDownloadJobStore jobStore,
        IProgressNotifier progressNotifier,
        ILogger<DownloadProcessor> logger,
        IOptions<DownloadSettings> downloadSettings)
    {
        _youtubeDownloader = youtubeDownloader;
        _jobStore = jobStore;
        _progressNotifier = progressNotifier;
        _logger = logger;
        _downloadSettings = downloadSettings.Value;
    }

    /// <summary>
    /// Processes a download job by job ID.
    /// </summary>
    public async Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _jobStore.GetAsync(jobId, cancellationToken);

        if (job is null)
        {
            _logger.LogError("Job {JobId} not found in store", jobId);
            return;
        }

        _logger.LogInformation(
            "Starting processing for job {JobId} with {TrackCount} track(s)",
            jobId,
            job.Tracks.Count);

        job.StartProcessing();
        await _jobStore.UpdateAsync(job, cancellationToken);
        await ReportProgressAsync(job, "Processing started", 0, cancellationToken);

        try
        {
            Stream resultStream;

            if (job.Tracks.Count == 1)
            {
                resultStream = await ProcessSingleTrackAsync(job, cancellationToken);
            }
            else
            {
                resultStream = await ProcessMultipleTracksAsync(job, cancellationToken);
            }

            // Save result stream
            await _jobStore.SaveResultStreamAsync(jobId, resultStream, cancellationToken);

            // Mark as completed
            job.Complete();
            await _jobStore.UpdateAsync(job, cancellationToken);

            // Send file to user
            var fileName = job.Tracks.Count == 1
                ? $"{SanitizeFileName(job.Tracks[0].Name)}.mp3"
                : $"{job.ItemType}_{jobId:N}.zip";

            var caption = job.Tracks.Count == 1
                ? $"*{job.Tracks[0].Name}*\n{string.Join(", ", job.Tracks[0].Artists)}"
                : $"*{job.ItemType}*\n{job.Tracks.Count} tracks";

            resultStream.Position = 0;
            await _progressNotifier.SendFileAsync(
                jobId,
                job.ChatId,
                resultStream,
                fileName,
                caption,
                cancellationToken);

            _logger.LogInformation("Job {JobId} completed successfully", jobId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Job {JobId} was cancelled", jobId);
            job.Fail("Job was cancelled");
            await _jobStore.UpdateAsync(job, cancellationToken);
            await _progressNotifier.SendErrorAsync(jobId, job.ChatId, "Download was cancelled", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", jobId);
            job.Fail(ex.Message);
            await _jobStore.UpdateAsync(job, cancellationToken);
            await _progressNotifier.SendErrorAsync(jobId, job.ChatId, ex.Message, cancellationToken);
        }
    }

    /// <summary>
    /// Processes a single track download.
    /// </summary>
    private async Task<Stream> ProcessSingleTrackAsync(
        DownloadJob job,
        CancellationToken cancellationToken)
    {
        var track = job.Tracks.First();
        var query = track.GetSearchQuery();

        _logger.LogInformation(
            "Downloading single track: {TrackName} for job {JobId}",
            track.Name,
            job.Id);

        var progress = new Progress<double>(async p =>
        {
            job.UpdateProgress(p, track.Name);
            await _jobStore.UpdateAsync(job, cancellationToken);
            await ReportProgressAsync(job, $"Downloading: {track.Name}", p, cancellationToken);
        });

        var result = await _youtubeDownloader.DownloadAsync(query, track, progress, cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to download track: {result.Error!.Message}");
        }

        job.CompleteTrack();
        await _jobStore.UpdateAsync(job, cancellationToken);

        return result.Value!;
    }

    /// <summary>
    /// Processes multiple tracks and creates a ZIP archive.
    /// Downloads multiple tracks in parallel.
    /// </summary>
    private async Task<Stream> ProcessMultipleTracksAsync(
        DownloadJob job,
        CancellationToken cancellationToken)
    {
        var maxConcurrentDownloads = _downloadSettings.MaxParallelJobs;

        _logger.LogInformation(
            "Downloading {TrackCount} tracks for job {JobId} with max {MaxConcurrency} concurrent downloads",
            job.Tracks.Count,
            job.Id,
            maxConcurrentDownloads);

        // Download all tracks concurrently
        var downloadedTracks = await DownloadTracksConcurrentlyAsync(
            job,
            maxConcurrentDownloads,
            cancellationToken);

        // Create ZIP from downloaded tracks
        var zipStream = await CreateZipFromDownloadedTracksAsync(
            job,
            downloadedTracks,
            cancellationToken);

        zipStream.Position = 0;
        return zipStream;
    }

    /// <summary>
    /// Downloads all tracks concurrently with configurable parallelism.
    /// </summary>
    private async Task<List<(int TrackNumber, SpotifyTrack Track, Stream? AudioStream, bool Success, string? Error)>>
        DownloadTracksConcurrentlyAsync(
            DownloadJob job,
            int maxConcurrentDownloads,
            CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(maxConcurrentDownloads, maxConcurrentDownloads);
        var downloadTasks = new List<Task<(int, SpotifyTrack, Stream?, bool, string?)>>();

        for (int i = 0; i < job.Tracks.Count; i++)
        {
            var trackNumber = i + 1;
            var track = job.Tracks[i];
            var trackIndex = i;

            var downloadTask = DownloadSingleTrackAsync(
                job,
                track,
                trackNumber,
                trackIndex,
                semaphore,
                cancellationToken);

            downloadTasks.Add(downloadTask);
        }

        var results = await Task.WhenAll(downloadTasks);

        var successCount = results.Count(r => r.Item4);
        _logger.LogInformation(
            "Concurrent downloads completed: {SuccessCount}/{TotalCount} successful",
            successCount,
            job.Tracks.Count);

        return results.ToList();
    }

    /// <summary>
    /// Downloads a single track with semaphore control for concurrency limiting.
    /// </summary>
    private async Task<(int TrackNumber, SpotifyTrack Track, Stream? AudioStream, bool Success, string? Error)>
        DownloadSingleTrackAsync(
            DownloadJob job,
            SpotifyTrack track,
            int trackNumber,
            int trackIndex,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            _logger.LogInformation(
                "Processing track {TrackNumber}/{TotalTracks}: {TrackName}",
                trackNumber,
                job.Tracks.Count,
                track.Name);

            var currentProgress = (double)trackIndex / job.Tracks.Count * 100;
            job.UpdateProgress(currentProgress, track.Name);
            await _jobStore.UpdateAsync(job, cancellationToken);

            if (_downloadSettings.EnableDetailedProgress)
            {
                await ReportProgressAsync(
                    job,
                    $"Downloading: {track.Name} ({trackNumber}/{job.Tracks.Count})",
                    job.Progress,
                    cancellationToken);
            }

            if (_downloadSettings.MinDelayBetweenDownloads > 0 && trackIndex > 0)
            {
                await Task.Delay(_downloadSettings.MinDelayBetweenDownloads, cancellationToken);
            }

            var result = await DownloadWithRetryAsync(track, trackNumber, trackIndex, job, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to download track {TrackName}: {Error}",
                    track.Name,
                    result.Error!.Message);

                return (trackNumber, track, null, false, result.Error.Message);
            }

            _logger.LogInformation(
                "Successfully downloaded track {TrackNumber}/{TotalTracks}: {TrackName}",
                trackNumber,
                job.Tracks.Count,
                track.Name);

            return (trackNumber, track, result.Value, true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Exception downloading track {TrackNumber}: {TrackName}",
                trackNumber,
                track.Name);

            return (trackNumber, track, null, false, ex.Message);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Downloads a track with retry logic and timeout.
    /// </summary>
    private async Task<Result<Stream>> DownloadWithRetryAsync(
        SpotifyTrack track,
        int trackNumber,
        int trackIndex,
        DownloadJob job,
        CancellationToken cancellationToken)
    {
        var query = track.GetSearchQuery();
        var maxAttempts = _downloadSettings.EnableRetry ? _downloadSettings.MaxRetryAttempts : 1;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            CancellationTokenSource? timeoutCts = null;
            CancellationTokenSource? linkedCts = null;

            try
            {
                if (attempt > 1)
                {
                    _logger.LogInformation(
                        "Retry attempt {Attempt}/{MaxAttempts} for track: {TrackName}",
                        attempt,
                        maxAttempts,
                        track.Name);

                    await Task.Delay(_downloadSettings.RetryDelayMilliseconds, cancellationToken);
                }

                var progress = new Progress<double>(async p =>
                {
                    var overallProgress = ((double)trackIndex + p / 100.0) / job.Tracks.Count * 100;
                    job.UpdateProgress(overallProgress, track.Name);
                    await _jobStore.UpdateAsync(job, cancellationToken);
                });

                timeoutCts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(_downloadSettings.DownloadTimeoutSeconds));
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    timeoutCts.Token);

                var result = await _youtubeDownloader.DownloadAsync(
                    query,
                    track,
                    progress,
                    linkedCts.Token);

                if (result.IsSuccess)
                {
                    if (attempt > 1)
                    {
                        _logger.LogInformation(
                            "Successfully downloaded track {TrackName} on attempt {Attempt}",
                            track.Name,
                            attempt);
                    }
                    return result;
                }

                if (attempt == maxAttempts)
                {
                    return result;
                }

                _logger.LogWarning(
                    "Attempt {Attempt}/{MaxAttempts} failed for track {TrackName}: {Error}",
                    attempt,
                    maxAttempts,
                    track.Name,
                    result.Error!.Message);
            }
            catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
            {
                if (attempt == maxAttempts)
                {
                    return Result<Stream>.Failure(Error.Failure(
                        "Download.Timeout",
                        $"Download timed out after {_downloadSettings.DownloadTimeoutSeconds} seconds"));
                }

                _logger.LogWarning(
                    "Attempt {Attempt}/{MaxAttempts} timed out for track {TrackName}",
                    attempt,
                    maxAttempts,
                    track.Name);
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Attempt {Attempt}/{MaxAttempts} threw exception for track {TrackName}",
                    attempt,
                    maxAttempts,
                    track.Name);
            }
            finally
            {
                linkedCts?.Dispose();
                timeoutCts?.Dispose();
            }
        }

        return Result<Stream>.Failure(Error.Failure(
            "Download.Failed",
            "Download failed after all retry attempts"));
    }

    /// <summary>
    /// Creates ZIP archive from downloaded tracks (successful ones).
    /// </summary>
    private async Task<MemoryStream> CreateZipFromDownloadedTracksAsync(
        DownloadJob job,
        List<(int TrackNumber, SpotifyTrack Track, Stream? AudioStream, bool Success, string? Error)> downloadedTracks,
        CancellationToken cancellationToken)
    {
        var zipStream = new MemoryStream();

        using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var successfulTracks = downloadedTracks
                .Where(t => t.Success && t.AudioStream != null)
                .OrderBy(t => t.TrackNumber)
                .ToList();

            foreach (var (trackNumber, track, audioStream, _, _) in successfulTracks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sanitizedName = SanitizeFileName(track.Name);
                var entry = zip.CreateEntry($"{trackNumber:D2} - {sanitizedName}.mp3");

                using var entryStream = entry.Open();
                audioStream!.Position = 0;
                await audioStream.CopyToAsync(entryStream, cancellationToken);

                job.CompleteTrack();
                await _jobStore.UpdateAsync(job, cancellationToken);

                _logger.LogInformation(
                    "Added track {TrackNumber}/{TotalTracks} to ZIP",
                    trackNumber,
                    job.Tracks.Count);
            }

            var failedTracks = downloadedTracks
                .Where(t => !t.Success)
                .OrderBy(t => t.TrackNumber)
                .ToList();

            if (failedTracks.Any())
            {
                var errorReport = GenerateErrorReport(failedTracks);
                var errorEntry = zip.CreateEntry("DOWNLOAD_ERRORS.txt");

                using var errorWriter = new StreamWriter(errorEntry.Open());
                await errorWriter.WriteAsync(errorReport);

                _logger.LogWarning(
                    "Added error report to ZIP: {FailedCount} tracks failed",
                    failedTracks.Count);
            }
        }

        return zipStream;
    }

    /// <summary>
    /// Generates a readable error report for failed downloads.
    /// </summary>
    private string GenerateErrorReport(
        List<(int TrackNumber, SpotifyTrack Track, Stream? AudioStream, bool Success, string? Error)> failedTracks)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("Download Error Report");
        report.AppendLine("=".PadRight(50, '='));
        report.AppendLine();
        report.AppendLine($"Failed downloads: {failedTracks.Count}");
        report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine();

        foreach (var (trackNumber, track, _, _, error) in failedTracks)
        {
            report.AppendLine($"Track #{trackNumber:D2}: {track.Name}");
            report.AppendLine($"  Artist: {string.Join(", ", track.Artists)}");
            report.AppendLine($"  Error: {error ?? "Unknown error"}");
            report.AppendLine();
        }

        return report.ToString();
    }

    /// <summary>
    /// Reports progress to the user.
    /// </summary>
    private async Task ReportProgressAsync(
        DownloadJob job,
        string status,
        double percentage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _progressNotifier.ReportProgressAsync(
                job.Id,
                job.ChatId,
                status,
                percentage,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send progress update for job {JobId}", job.Id);
        }
    }

    /// <summary>
    /// Sanitizes a filename by removing invalid characters.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 100 ? sanitized[..100] : sanitized;
    }
}
