

using System.Net;

namespace YTapi.Infrastructure.Proxies;
/// <summary>
/// No-op proxy provider for when proxy is disabled or not needed.
/// </summary>
public sealed class NullProxyProvider : IProxyProvider
{
    public WebProxy? GetProxy()
    {
        throw new InvalidOperationException("No proxy configured. Use NullProxyProvider.IsAvailable() to check first.");
    }

    public string GetProxyInfo()
    {
        return "No proxy configured";
    }

    public bool IsAvailable()
    {
        return false;
    }
}