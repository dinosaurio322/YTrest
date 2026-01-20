namespace YTapi.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for the download system.
/// </summary>
public sealed class DownloadSettings
{
    /// <summary>
    /// Maximum number of concurrent track downloads.
    /// Default: 4
    /// </summary>
    public int MaxConcurrentDownloads { get; init; } = 4;

    /// <summary>
    /// Maximum number of parallel jobs being processed.
    /// Default: 2
    /// </summary>
    public int MaxParallelJobs { get; init; } = 2;

    /// <summary>
    /// Minimum delay between download operations (milliseconds).
    /// Helps prevent rate limiting.
    /// Default: 100ms
    /// </summary>
    public int MinDelayBetweenDownloads { get; init; } = 100;

    /// <summary>
    /// Timeout for individual track downloads (seconds).
    /// Default: 300 seconds (5 minutes)
    /// </summary>
    public int DownloadTimeoutSeconds { get; init; } = 300;

    /// <summary>
    /// Enable retry logic for failed downloads.
    /// Default: true
    /// </summary>
    public bool EnableRetry { get; init; } = true;

    /// <summary>
    /// Maximum retry attempts per track.
    /// Default: 3
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Delay between retry attempts (milliseconds).
    /// Default: 2000ms (2 seconds)
    /// </summary>
    public int RetryDelayMilliseconds { get; init; } = 2000;

    /// <summary>
    /// Enable detailed progress reporting for each concurrent download.
    /// May increase SignalR traffic.
    /// Default: true
    /// </summary>
    public bool EnableDetailedProgress { get; init; } = true;

    /// <summary>
    /// Buffer size for concurrent operations (MB).
    /// Default: 512MB
    /// </summary>
    public int BufferSizeMb { get; init; } = 512;
}