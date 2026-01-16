using System.Net;
using Microsoft.Extensions.Options;
using YoutubeExplode;
using YTapi.Infrastructure.Configuration;
using YTapi.Infrastructure.ExternalServices.Interfaces;

namespace YTapi.Infrastructure.ExternalServices.YouTube;
 
/// <summary>
/// Factory implementation for creating YouTube clients with proper configuration.
/// </summary>
public sealed class YoutubeClientFactory : IYoutubeClientFactory
{
    private readonly YouTubeSettings _settings;
    private readonly TimeSpan _timeout;

    public YoutubeClientFactory(IOptions<YouTubeSettings> options)
    {
        _settings = options.Value;
        _timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    /// <summary>
    /// Creates a new YouTube client with the specified proxy.
    /// </summary>
    public YoutubeClient Create(WebProxy? proxy = null)
    {
       var handler = new HttpClientHandler
        {
            Proxy = proxy,
            UseProxy = proxy != null,
            //UseCookies = true,
            //CookieContainer = new CookieContainer(),
            //AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            //AllowAutoRedirect = true,
            //MaxAutomaticRedirections = 10
        };


        // Create HTTP client with timeout
        var httpClient = new HttpClient(handler)
        {
            Timeout = _timeout
        };

        httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");

        return new YoutubeClient(httpClient);
    }
}