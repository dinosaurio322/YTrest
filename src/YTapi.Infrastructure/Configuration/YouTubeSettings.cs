namespace YTapi.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for YouTube integration.
/// </summary>
public sealed class YouTubeSettings
{
    /// <summary>
    /// Timeout in seconds for HTTP requests to YouTube.
    /// Default: 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for failed requests.
    /// Default: 3 retries.
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Delay in milliseconds between retry attempts.
    /// Default: 1000ms (1 second).
    /// </summary>
    public int RetryDelayMilliseconds { get; init; } = 1000;

    /// <summary>
    /// Whether to use exponential backoff for retries.
    /// Default: true.
    /// </summary>
    public bool UseExponentialBackoff { get; init; } = true;

    /// <summary>
    /// Validates that all settings are within acceptable ranges.
    /// </summary>
    public void Validate()
    {
        if (TimeoutSeconds < 5 || TimeoutSeconds > 300)
            throw new InvalidOperationException("TimeoutSeconds must be between 5 and 300.");

        if (MaxRetries < 0 || MaxRetries > 10)
            throw new InvalidOperationException("MaxRetries must be between 0 and 10.");

        if (RetryDelayMilliseconds < 100 || RetryDelayMilliseconds > 10000)
            throw new InvalidOperationException("RetryDelayMilliseconds must be between 100 and 10000.");
    }
}