namespace YTapi.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for FFmpeg audio conversion.
/// </summary>
public sealed class FFmpegSettings
{
    /// <summary>
    /// Audio bitrate in kbps for MP3 conversion.
    /// Default: 192 kbps.
    /// </summary>
    public int AudioBitrate { get; init; } = 192;

    /// <summary>
    /// Output audio format.
    /// Default: "mp3".
    /// </summary>
    public string Format { get; init; } = "mp3";

    /// <summary>
    /// Audio quality preset (0-9, where 0 is best and 9 is worst).
    /// Default: 2 (high quality).
    /// </summary>
    public int Quality { get; init; } = 2;

    /// <summary>
    /// Sample rate in Hz.
    /// Default: 44100 Hz (CD quality).
    /// </summary>
    public int SampleRate { get; init; } = 44100;

    /// <summary>
    /// Number of audio channels.
    /// Default: 2 (stereo).
    /// </summary>
    public int Channels { get; init; } = 2;

    /// <summary>
    /// Whether to normalize audio volume.
    /// Default: false.
    /// </summary>
    public bool NormalizeAudio { get; init; } = false;

    /// <summary>
    /// Whether to embed album art in the output file.
    /// Default: true.
    /// </summary>
    public bool EmbedAlbumArt { get; init; } = true;

    /// <summary>
    /// Validates that all settings are within acceptable ranges.
    /// </summary>
    public void Validate()
    {
        if (AudioBitrate < 64 || AudioBitrate > 320)
            throw new InvalidOperationException("AudioBitrate must be between 64 and 320 kbps.");

        if (Quality < 0 || Quality > 9)
            throw new InvalidOperationException("Quality must be between 0 and 9.");

        if (SampleRate < 8000 || SampleRate > 192000)
            throw new InvalidOperationException("SampleRate must be between 8000 and 192000 Hz.");

        if (Channels < 1 || Channels > 2)
            throw new InvalidOperationException("Channels must be 1 (mono) or 2 (stereo).");

        var validFormats = new[] { "mp3", "wav", "flac", "aac" };
        if (!validFormats.Contains(Format.ToLower()))
            throw new InvalidOperationException($"Format must be one of: {string.Join(", ", validFormats)}.");
    }
}