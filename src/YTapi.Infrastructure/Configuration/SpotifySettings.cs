namespace YTapi.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Spotify API integration.
/// </summary>
public sealed class SpotifySettings
{
    /// <summary>
    /// Spotify Client ID from Developer Dashboard.
    /// </summary>
    public required string ClientId { get; init; }

    /// <summary>
    /// Spotify Client Secret from Developer Dashboard.
    /// </summary>
    public required string ClientSecret { get; init; }

    /// <summary>
    /// Buffer time in seconds before token expiration to trigger refresh.
    /// Default: 60 seconds.
    /// </summary>
    public int TokenRefreshBufferSeconds { get; init; } = 60;

    /// <summary>
    /// Validates that all required settings are configured.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
            throw new InvalidOperationException("Spotify ClientId is not configured.");

        if (string.IsNullOrWhiteSpace(ClientSecret))
            throw new InvalidOperationException("Spotify ClientSecret is not configured.");

        if (TokenRefreshBufferSeconds < 0 || TokenRefreshBufferSeconds > 3600)
            throw new InvalidOperationException("TokenRefreshBufferSeconds must be between 0 and 3600.");
    }
}