using System.IO.Compression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging; 
using YTapi.Application.DTOs.Responses;
using YTapi.Application.Interfaces;
using YTapi.Domain.Entities;
using YTapi.Domain.Enums;
using YTapi.Infrastructure.Hubs;

namespace YTapi.Infrastructure.BackgroundJobs;

/// <summary>
/// Processes download jobs by downloading audio from YouTube and converting to MP3.
/// Handles both single track and multi-track (album) downloads.
/// </summary>
public sealed class DownloadProcessor : IDownloadProcessor
{
    private readonly IYoutubeAudioDownloader _youtubeDownloader;
    private readonly IDownloadJobStore _jobStore;
    private readonly IHubContext<DownloadHub> _hubContext;
    private readonly ILogger<DownloadProcessor> _logger;

    public DownloadProcessor(
        IYoutubeAudioDownloader youtubeDownloader,
        IDownloadJobStore jobStore,
        IHubContext<DownloadHub> hubContext,
        ILogger<DownloadProcessor> logger)
    {
        _youtubeDownloader = youtubeDownloader;
        _jobStore = jobStore;
        _hubContext = hubContext;
        _logger = logger;
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
        await ReportProgressAsync(jobId, "Processing started", 0);

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
            await ReportProgressAsync(jobId, "Completed", 100);

            _logger.LogInformation("Job {JobId} completed successfully", jobId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Job {JobId} was cancelled", jobId);
            job.Fail("Job was cancelled");
            await _jobStore.UpdateAsync(job, cancellationToken);
            await ReportProgressAsync(jobId, "Cancelled", job.Progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}", jobId);
            job.Fail(ex.Message);
            await _jobStore.UpdateAsync(job, cancellationToken);
            await ReportProgressAsync(jobId, $"Failed: {ex.Message}", job.Progress);
        }
    }

    /// <summary>
    /// Processes a single track download.
    /// </summary>
    private async Task<Stream> ProcessSingleTrackAsync(
        Domain.Entities.DownloadJob job,
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
            await ReportProgressAsync(job.Id, $"Downloading: {track.Name}", p);
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
    /// </summary>
    private async Task<Stream> ProcessMultipleTracksAsync(
        Domain.Entities.DownloadJob job,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Downloading {TrackCount} tracks for job {JobId}",
            job.Tracks.Count,
            job.Id);

        var zipStream = new MemoryStream();

        using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (int i = 0; i < job.Tracks.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var track = job.Tracks[i];
                var trackNumber = i + 1;

                _logger.LogInformation(
                    "Processing track {TrackNumber}/{TotalTracks}: {TrackName}",
                    trackNumber,
                    job.Tracks.Count,
                    track.Name);

                job.UpdateProgress(
                    (double)i / job.Tracks.Count * 100,
                    track.Name);
                await _jobStore.UpdateAsync(job, cancellationToken);
                await ReportProgressAsync(
                    job.Id,
                    $"Downloading: {track.Name} ({trackNumber}/{job.Tracks.Count})",
                    job.Progress);

                // Download track
                var query = track.GetSearchQuery();
                var progress = new Progress<double>(async p =>
                {
                    var overallProgress = ((double)i + p / 100.0) / job.Tracks.Count * 100;
                    job.UpdateProgress(overallProgress, track.Name);
                    await _jobStore.UpdateAsync(job, cancellationToken);
                });

                var result = await _youtubeDownloader.DownloadAsync(query, track, progress, cancellationToken);

                if (result.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to download track {TrackName}: {Error}. Skipping...",
                        track.Name,
                        result.Error!.Message);
                    continue; // Skip this track and continue with others
                }

                var audioStream = result.Value!;

                // Add to ZIP
                var sanitizedName = SanitizeFileName(track.Name);
                var entry = zip.CreateEntry($"{trackNumber:D2} - {sanitizedName}.mp3");

                using var entryStream = entry.Open();
                audioStream.Position = 0;
                await audioStream.CopyToAsync(entryStream, cancellationToken);

                job.CompleteTrack();
                await _jobStore.UpdateAsync(job, cancellationToken);

                _logger.LogInformation(
                    "Added track {TrackNumber}/{TotalTracks} to ZIP",
                    trackNumber,
                    job.Tracks.Count);
            }
        }

        zipStream.Position = 0;
        return zipStream;
    }

    /// <summary>
    /// Reports progress to connected clients via SignalR.
    /// </summary>
    private async Task ReportProgressAsync(Guid jobId, string status, double percentage)
    {
        try
        {
            await _hubContext.Clients.Group(jobId.ToString())
                .SendAsync("progress", new DownloadProgressDto
                {
                    JobId = jobId,
                    Status = status,
                    Percentage = Math.Round(percentage, 2)
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send progress update for job {JobId}", jobId);
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