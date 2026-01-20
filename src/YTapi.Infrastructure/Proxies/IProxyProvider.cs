using System.Net;

namespace YTapi.Infrastructure.Proxies;

/// <summary>
/// Provides proxy instances for HTTP requests.
/// </summary>
public interface IProxyProvider
{
    /// <summary>
    /// Gets the next available proxy.
    /// For single proxy implementations, always returns the same proxy.
    /// For proxy pool implementations, returns the next proxy in rotation.
    /// </summary>
    /// <returns>A configured WebProxy instance.</returns>
    WebProxy? GetProxy();

    /// <summary>
    /// Gets information about the current proxy.
    /// </summary>
    /// <returns>Proxy URL as string.</returns>
    string GetProxyInfo();

    /// <summary>
    /// Checks if the proxy provider is configured and ready.
    /// </summary>
    /// <returns>True if proxy is available, false otherwise.</returns>
    bool IsAvailable();
}