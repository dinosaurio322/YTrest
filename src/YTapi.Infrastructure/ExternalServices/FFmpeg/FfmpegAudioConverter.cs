using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YTapi.Application.Interfaces;
using YTapi.Domain.Common;
using YTapi.Domain.ValueObjects;
using YTapi.Infrastructure.Configuration;

namespace YTapi.Infrastructure.ExternalServices.FFmpeg;

/// <summary>
/// Converts audio streams to MP3 format with metadata using FFmpeg.
/// </summary>
public sealed class FfmpegAudioConverter : IAudioConverter
{
    private readonly HttpClient _httpClient;
    private readonly FFmpegSettings _settings;
    private readonly ILogger<FfmpegAudioConverter> _logger;

    public FfmpegAudioConverter(
        HttpClient httpClient,
        IOptions<FFmpegSettings> settings,
        ILogger<FfmpegAudioConverter> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Converts an audio stream to MP3 with metadata and optional album art.
    /// </summary>
    public async Task<Result<Stream>> ConvertToMp3Async(
        Stream input,
        SpotifyTrack metadata,
        IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(metadata);

        _logger.LogInformation("Starting MP3 conversion for: {TrackName}", metadata.Name);

        string? tempInputPath = null;
        string? tempOutputPath = null;
        string? coverImagePath = null;

        try
        {
            // Save input stream to temporary file
            tempInputPath = await SaveInputStreamAsync(input, cancellationToken);
            tempOutputPath = Path.ChangeExtension(tempInputPath, ".mp3");

            _logger.LogDebug("Temporary files created: Input={Input}, Output={Output}",
                Path.GetFileName(tempInputPath),
                Path.GetFileName(tempOutputPath));

            // Download cover art if available
            if (_settings.EmbedAlbumArt && !string.IsNullOrWhiteSpace(metadata.CoverUrl))
            {
                coverImagePath = await DownloadCoverImageAsync(metadata.CoverUrl, cancellationToken);
            }

            // Convert with FFmpeg
            await ConvertWithFfmpegAsync(
                tempInputPath,
                tempOutputPath,
                metadata,
                coverImagePath,
                progress);

            // Read result into memory
            var resultStream = new MemoryStream();
            using (var fileStream = File.OpenRead(tempOutputPath))
            {
                await fileStream.CopyToAsync(resultStream, cancellationToken);
            }

            resultStream.Position = 0;

            _logger.LogInformation(
                "Successfully converted to MP3: {TrackName} ({Size:N0} bytes)",
                metadata.Name,
                resultStream.Length);

            return Result<Stream>.Success(resultStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting audio to MP3 for: {TrackName}", metadata.Name);
            return Result<Stream>.Failure(
                Error.Failure("FFmpeg.ConversionError", $"Failed to convert audio: {ex.Message}"));
        }
        finally
        {
            // Cleanup temporary files
            CleanupTempFile(tempInputPath);
            CleanupTempFile(tempOutputPath);
            CleanupTempFile(coverImagePath);
        }
    }

    /// <summary>
    /// Saves the input stream to a temporary file.
    /// </summary>
    private static async Task<string> SaveInputStreamAsync(Stream input, CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"ytapi_input_{Guid.NewGuid()}.tmp");
        
        await using var fileStream = File.Create(tempPath);
        input.Position = 0;
        await input.CopyToAsync(fileStream, cancellationToken);
        
        return tempPath;
    }

    /// <summary>
    /// Downloads the cover image from URL.
    /// </summary>
    private async Task<string?> DownloadCoverImageAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Downloading cover image from: {Url}", url);

            var imageBytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);
            var imagePath = Path.Combine(Path.GetTempPath(), $"ytapi_cover_{Guid.NewGuid()}.jpg");
            
            await File.WriteAllBytesAsync(imagePath, imageBytes, cancellationToken);

            _logger.LogDebug("Cover image downloaded: {Size:N0} bytes", imageBytes.Length);
            return imagePath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download cover image, continuing without it");
            return null;
        }
    }

    /// <summary>
    /// Performs the actual conversion using FFmpeg.
    /// </summary>
    /// <summary>
/// Performs the actual conversion using FFmpeg.
/// </summary>
private async Task ConvertWithFfmpegAsync(
    string inputPath,
    string outputPath,
    SpotifyTrack metadata,
    string? coverImagePath,
    IProgress<double> progress)
{
    try
    {
        bool result;

        if (coverImagePath is not null)
        {
            // Convert with cover art
            result = await FFMpegArguments
                .FromFileInput(inputPath)
                .AddFileInput(coverImagePath)
                .OutputToFile(outputPath, overwrite: true, options => ConfigureOutput(options, metadata, true))
                .ProcessAsynchronously(throwOnError: true);
        }
        else
        {
            // Convert without cover art
            result = await FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(outputPath, overwrite: true, options => ConfigureOutput(options, metadata, false))
                .ProcessAsynchronously(throwOnError: true);
        }

        if (!result)
        {
            throw new InvalidOperationException("FFmpeg conversion failed");
        }

        _logger.LogDebug("FFmpeg conversion completed successfully");
        progress?.Report(100);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "FFmpeg processing error");
        throw;
    }
}
    /// <summary>
    /// Configures FFmpeg output options.
    /// </summary>
    private void ConfigureOutput(
        FFMpegArgumentOptions options,
        SpotifyTrack metadata,
        bool hasCoverArt)
    {
        // Set audio codec and bitrate
        options
            .WithAudioCodec("libmp3lame")
            .WithAudioBitrate(_settings.AudioBitrate)
            .WithCustomArgument($"-q:a {_settings.Quality}");

        // Set sample rate and channels
        if (_settings.SampleRate > 0)
        {
            options.WithAudioSamplingRate(_settings.SampleRate);
        }

        // Set ID3v2 version for better compatibility
        options.WithCustomArgument("-id3v2_version 3");

        // Add metadata tags
        options
            .WithCustomArgument($"-metadata title=\"{EscapeMetadata(metadata.Name)}\"")
            .WithCustomArgument($"-metadata album=\"{EscapeMetadata(metadata.Album)}\"")
            .WithCustomArgument($"-metadata artist=\"{EscapeMetadata(string.Join(", ", metadata.Artists))}\"");

        // Add duration if available
        if (metadata.DurationMs > 0)
        {
            var duration = TimeSpan.FromMilliseconds(metadata.DurationMs);
            options.WithCustomArgument($"-metadata duration=\"{duration}\"");
        }

        // Handle cover art embedding
        if (hasCoverArt)
        {
            options
                .WithCustomArgument("-map 0:a")  // Map audio from first input
                .WithCustomArgument("-map 1:v")  // Map video (image) from second input
                .WithCustomArgument("-c:v copy") // Copy image without re-encoding
                .WithCustomArgument("-metadata:s:v title=\"Album cover\"")
                .WithCustomArgument("-metadata:s:v comment=\"Cover (front)\"")
                .WithCustomArgument("-disposition:v:0 attached_pic"); // Mark as attached picture
        }

        // Audio normalization if enabled
        if (_settings.NormalizeAudio)
        {
            options.WithCustomArgument("-af loudnorm");
        }
    }

    /// <summary>
    /// Escapes metadata strings for FFmpeg.
    /// </summary>
    private static string EscapeMetadata(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("'", "\\'");
    }

    /// <summary>
    /// Safely deletes a temporary file.
    /// </summary>
    private void CleanupTempFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                _logger.LogDebug("Cleaned up temporary file: {File}", Path.GetFileName(path));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup temporary file: {File}", path);
        }
    }
}