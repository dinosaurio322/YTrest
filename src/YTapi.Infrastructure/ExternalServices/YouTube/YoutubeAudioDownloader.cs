using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YoutubeExplode;
using YoutubeExplode.Exceptions;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;
using YTapi.Application.Interfaces;
using YTapi.Domain.Common;
using YTapi.Domain.ValueObjects;
using YTapi.Infrastructure.Configuration;
using YTapi.Infrastructure.ExternalServices.Interfaces;
using YTapi.Infrastructure.Proxies;

namespace YTapi.Infrastructure.ExternalServices.YouTube;

/// <summary>
/// Downloads and converts audio from YouTube based on search queries.
/// </summary>
public sealed class YoutubeAudioDownloader : IYoutubeAudioDownloader
{
    private readonly IYoutubeClientFactory _clientFactory;
    private readonly IProxyProvider _proxyProvider;
    private readonly IAudioConverter _audioConverter;
    private readonly YouTubeSettings _settings;
    private readonly ILogger<YoutubeAudioDownloader> _logger;

    public YoutubeAudioDownloader(
        IYoutubeClientFactory clientFactory,
        IProxyProvider proxyProvider,
        IAudioConverter audioConverter,
        IOptions<YouTubeSettings> settings,
        ILogger<YoutubeAudioDownloader> logger)
    {
        _clientFactory = clientFactory;
        _proxyProvider = proxyProvider;
        _audioConverter = audioConverter;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Downloads audio from YouTube and converts it to MP3.
    /// </summary>
    public async Task<Result<Stream>> DownloadAsync(
        string query,
        SpotifyTrack metadata,
        IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Result<Stream>.Failure(
                Error.Validation("YouTube.InvalidQuery", "Search query cannot be empty"));
        }

        ArgumentNullException.ThrowIfNull(metadata);

        _logger.LogInformation("Starting YouTube download for query: {Query}", query);

        var proxy = _proxyProvider.IsAvailable() ? _proxyProvider.GetProxy() : null;
        
        if (proxy != null)
        {
            _logger.LogInformation(
                "Using rotating proxy: {ProxyInfo}", 
                _proxyProvider.GetProxyInfo());
        }
        else
        {
            _logger.LogInformation("Using direct connection (no proxy)");
        }

        try
        {
            // Step 1: Find video
            _logger.LogDebug("Searching for video: {Query}", query);
            var videoResult = await FindVideoAsync(query, proxy, cancellationToken);

            if (videoResult.IsFailure)
            {
                return Result<Stream>.Failure(videoResult.Error!);
            }

            var video = videoResult.Value!;
            _logger.LogInformation("Found video: {Title} ({Id})", video.Title, video.Id);

            // Step 2: Get audio stream with retry logic
            _logger.LogDebug("Getting audio stream for video: {VideoId}", video.Id);
            var audioStreamResult = await TryGetAudioStreamAsync(video.Id, proxy, cancellationToken);

            if (audioStreamResult.IsFailure)
            {
                return Result<Stream>.Failure(audioStreamResult.Error!);
            }

            var audioStream = audioStreamResult.Value!;
            _logger.LogInformation(
                "Successfully obtained audio stream: {Container} - {Bitrate}kbps",
                audioStream.Container.Name,
                audioStream.Bitrate.KiloBitsPerSecond);

            // Step 3: Download audio stream
            _logger.LogDebug("Downloading audio stream...");
            var downloadedStream = await DownloadAudioStreamAsync(
                video.Id, 
                audioStream,
                proxy,
                progress,
                cancellationToken);

            if (downloadedStream.IsFailure)
            {
                return Result<Stream>.Failure(downloadedStream.Error!);
            }

            // Step 4: Convert to MP3
            _logger.LogDebug("Converting audio to MP3...");
            var mp3Result = await _audioConverter.ConvertToMp3Async(
                downloadedStream.Value!,
                metadata,
                progress,
                cancellationToken);

            if (mp3Result.IsFailure)
            {
                downloadedStream.Value?.Dispose();
                return Result<Stream>.Failure(mp3Result.Error!);
            }

            _logger.LogInformation("Successfully downloaded and converted: {TrackName}", metadata.Name);
            return mp3Result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Download was cancelled for query: {Query}", query);
            return Result<Stream>.Failure(
                Error.Failure("YouTube.Cancelled", "Download was cancelled"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error downloading audio for query: {Query}", query);
            return Result<Stream>.Failure(
                Error.Failure("YouTube.UnexpectedError", $"An unexpected error occurred: {ex.Message}"));
        }
    }

    /// <summary>
    /// Finds a video on YouTube matching the search query.
    /// </summary>
    private async Task<Result<VideoSearchResult>> FindVideoAsync(
        string query, WebProxy? proxy,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.Create(proxy);

            await foreach (var video in client.Search.GetVideosAsync(query, cancellationToken))
            {
                // Return first result
                return Result<VideoSearchResult>.Success(video);
            }

            _logger.LogWarning("No video found for query: {Query}", query);
            return Result<VideoSearchResult>.Failure(
                Error.NotFound("YouTube.VideoNotFound", $"No video found for query: {query}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for video: {Query}", query);
            return Result<VideoSearchResult>.Failure(
                Error.Failure("YouTube.SearchError", "Failed to search for video"));
        }
    }

    /// <summary>
    /// Attempts to get audio stream with retry logic and proxy rotation.
    /// </summary>
    private async Task<Result<IStreamInfo>> TryGetAudioStreamAsync(
        string videoId, WebProxy? proxy,
        CancellationToken cancellationToken)
    {
        var maxRetries = _settings.MaxRetries;
        var retryDelay = TimeSpan.FromMilliseconds(_settings.RetryDelayMilliseconds);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            { 
                var client = _clientFactory.Create(proxy);
                var manifest = await client.Videos.Streams.GetManifestAsync(videoId, cancellationToken);

                var audioStream = manifest
                    .GetAudioOnlyStreams()
                    .Where(s => s.Container.Name == "webm" || s.Container.Name == "mp4")
                    .OrderByDescending(s => s.Bitrate)
                    .FirstOrDefault();

                if (audioStream is null)
                {
                    return Result<IStreamInfo>.Failure(
                        Error.NotFound("YouTube.NoAudioStream", "No suitable audio stream found"));
                }

                _logger.LogInformation(
                    "Successfully obtained audio stream on attempt {Attempt}",
                    attempt);

                return Result<IStreamInfo>.Success(audioStream);
            }
            catch (VideoUnavailableException ex)
            {
                _logger.LogWarning(
                    "Video unavailable (attempt {Attempt}/{MaxRetries}): {Message}",
                    attempt,
                    maxRetries,
                    ex.Message);

                if (attempt == maxRetries)
                {
                    return Result<IStreamInfo>.Failure(
                        Error.NotFound("YouTube.VideoUnavailable", "Video is unavailable or blocked"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error getting audio stream (attempt {Attempt}/{MaxRetries})",
                    attempt,
                    maxRetries);

                if (attempt == maxRetries)
                {
                    return Result<IStreamInfo>.Failure(
                        Error.Failure("YouTube.StreamError", "Failed to get audio stream after multiple attempts"));
                }
            }

            // Calculate delay with exponential backoff if enabled
            var delay = _settings.UseExponentialBackoff
                ? TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1))
                : retryDelay;

            _logger.LogDebug("Waiting {Delay}ms before retry...", delay.TotalMilliseconds);
            await Task.Delay(delay, cancellationToken);
        }

        return Result<IStreamInfo>.Failure(
            Error.Failure("YouTube.MaxRetriesExceeded", "Failed to get audio stream after all retry attempts"));
    }

    /// <summary>
    /// Downloads the audio stream to memory.
    /// </summary>
    private async Task<Result<Stream>> DownloadAudioStreamAsync(
        string videoId,
        IStreamInfo streamInfo,
        WebProxy? proxy,
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _clientFactory.Create(proxy);

            var memoryStream = new MemoryStream();
            
            await client.Videos.Streams.CopyToAsync(
                streamInfo,
                memoryStream,
                progress,
                cancellationToken);

            memoryStream.Position = 0;

            _logger.LogInformation(
                "Downloaded {Size:N0} bytes from YouTube",
                memoryStream.Length);

            return Result<Stream>.Success(memoryStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading audio stream for video: {VideoId}", videoId);
            return Result<Stream>.Failure(
                Error.Failure("YouTube.DownloadError", "Failed to download audio stream"));
        }
    }
}