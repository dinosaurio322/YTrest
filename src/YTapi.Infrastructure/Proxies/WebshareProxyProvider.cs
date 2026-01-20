

using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YTapi.Infrastructure.Configuration;

namespace YTapi.Infrastructure.Proxies;

public sealed class WebshareProxyProvider : IProxyProvider
{
    private readonly ProxySettings _settings;
    private readonly WebProxy? _proxy;
    private readonly ILogger<WebshareProxyProvider> _logger;
    private readonly bool _isConfigured;

    public WebshareProxyProvider(
        IOptions<ProxySettings> options,
        ILogger<WebshareProxyProvider> logger)
    {
        _settings = options.Value;
        _logger = logger;

        try
        {
            if (_settings.Enabled)
            {
                _settings.Validate();
                _proxy = CreateProxy();
                _isConfigured = true;
                
                _logger.LogInformation(
                    "Webshare rotating proxy initialized: {ProxyUrl}",
                    _settings.GetProxyUrl());
            }
            else
            {
                _proxy = null;
                _isConfigured = false;
                _logger.LogInformation("Proxy disabled - direct connections will be used");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Webshare proxy provider");
            _isConfigured = false;
            _proxy = null;
        }
    }

    /// <summary>
    /// Gets the configured rotating proxy.
    /// Returns null if proxy is disabled.
    /// Note: Webshare handles IP rotation automatically.
    /// </summary>
    public WebProxy? GetProxy()
    {
        if (!_isConfigured || _proxy is null)
        {
            _logger.LogDebug("Proxy is disabled, returning null");
            return null;
        }

        return _proxy;
    }

    /// <summary>
    /// Gets information about the current proxy configuration.
    /// </summary>
    public string GetProxyInfo()
    {
        if (!_isConfigured || !_settings.Enabled)
        {
            return "Proxy: Disabled";
        }

        return $"{_settings.Provider} (Rotating): {_settings.GetProxyUrl()}";
    }

    /// <summary>
    /// Checks if the proxy is properly configured and available.
    /// </summary>
    public bool IsAvailable()
    {
        return _isConfigured && _proxy is not null && _settings.Enabled;
    }

    /// <summary>
    /// Creates and configures a WebProxy instance for Webshare rotating proxy.
    /// </summary>
    private WebProxy CreateProxy()
    {
        var proxyUri = new Uri(_settings.GetProxyUrl());
        var proxy = new WebProxy(proxyUri)
        {
            UseDefaultCredentials = false
        };

        // Set credentials if authentication is required
        if (_settings.RequiresAuthentication)
        {
            proxy.Credentials = new NetworkCredential(
                _settings.Username,
                _settings.Password);
            
            _logger.LogDebug(
                "Rotating proxy configured with authentication for user: {Username}",
                _settings.Username);
        }

        // Configure bypass list if specified
        if (_settings.BypassList.Any())
        {
            proxy.BypassList = _settings.BypassList.ToArray();
            
            //_logger.LogDebug(
              //  "Proxy bypass list configured with {Count} entries",
               // _settings.BypassList.Count);
        }

        return proxy;
    }
}
 