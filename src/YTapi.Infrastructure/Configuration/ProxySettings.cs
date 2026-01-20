namespace YTapi.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for proxy provider.
/// </summary>
public sealed class ProxySettings
{
    /// <summary>
    /// It was added only for testing purposes.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Proxy provider name (e.g., "Webshare", "Custom").
    /// Default: "Webshare".
    /// </summary>
    public string Provider { get; init; } = "Webshare";

    /// <summary>
    /// Proxy server hostname or IP address.
    /// Required.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// Proxy server port.
    /// Default: 80.
    /// </summary>
    public int Port { get; init; } = 80;

    /// <summary>
    /// Proxy authentication username.
    /// Required.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Proxy authentication password.
    /// Required.
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Whether to use HTTPS for proxy connection.
    /// Default: false.
    /// </summary>
    public bool UseHttps { get; init; } = false;

    /// <summary>
    /// Whether proxy authentication is required.
    /// Default: true.
    /// </summary>
    public bool RequiresAuthentication { get; init; } = true;

    /// <summary>
    /// List of domains to bypass proxy (direct connection).
    /// </summary>
    public string[] BypassList { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the full proxy URL.
    /// </summary>
    public string GetProxyUrl()
    {
        var protocol = UseHttps ? "https" : "http";
        return $"{protocol}://{Host}:{Port}";
    }

    /// <summary>
    /// Validates that all required settings are configured.
    /// </summary>
    public void Validate()
    {
        if (!Enabled) return;

        if (string.IsNullOrWhiteSpace(Host))
            throw new InvalidOperationException("Proxy Host is not configured.");

        if (Port < 1 || Port > 65535)
            throw new InvalidOperationException("Proxy Port must be between 1 and 65535.");

        if (RequiresAuthentication)
        {
            if (string.IsNullOrWhiteSpace(Username))
                throw new InvalidOperationException("Proxy Username is required when authentication is enabled.");

            if (string.IsNullOrWhiteSpace(Password))
                throw new InvalidOperationException("Proxy Password is required when authentication is enabled.");
        }
    }
}