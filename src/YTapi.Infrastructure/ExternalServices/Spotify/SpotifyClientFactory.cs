using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using YTapi.Infrastructure.Configuration;
using YTapi.Infrastructure.ExternalServices.Interfaces;

namespace YTapi.Infrastructure.ExternalServices.Spotify;


public sealed class SpotifyClientFactory : ISpotifyClientFactory, IDisposable
{
    private readonly SpotifySettings _settings;
    private readonly ILogger<SpotifyClientFactory> _logger;
    private Timer? _refreshTimer;
    private SpotifyClient? _client;

    public SpotifyClientFactory(
        IOptions<SpotifySettings> options,
        ILogger<SpotifyClientFactory> logger)
    {
        _settings = options.Value;
        _logger = logger;
        _refreshTimer = new Timer(
            async _ => await RefreshTokenAsync(),
            null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
    }

    public SpotifyClient Client => _client
        ?? throw new InvalidOperationException("Spotify client not initialized. Call InitializeAsync first.");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId) ||
            string.IsNullOrWhiteSpace(_settings.ClientSecret))
        {
            throw new InvalidOperationException("Spotify ClientId or ClientSecret not configured.");
        }

        await RefreshTokenAsync(cancellationToken);
    }

    private async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = SpotifyClientConfig.CreateDefault();
            var oauth = new OAuthClient(config);
            var tokenRequest = new ClientCredentialsRequest(_settings.ClientId, _settings.ClientSecret);
            var token = await oauth.RequestToken(tokenRequest);

            _client = new SpotifyClient(config.WithToken(token.AccessToken));

            _logger.LogInformation(
                "Spotify token refreshed successfully. Expires in {ExpiresIn} seconds",
                token.ExpiresIn);
 
            var refreshTime = TimeSpan.FromSeconds(token.ExpiresIn - _settings.TokenRefreshBufferSeconds);
 
            if (_refreshTimer == null)
            {
                _refreshTimer = new Timer(
                    async _ => await RefreshTokenAsync(),
                    null,
                    refreshTime,
                    Timeout.InfiniteTimeSpan);
            }
            else
            {
                _refreshTimer.Change(refreshTime, Timeout.InfiniteTimeSpan);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Spotify token");
 
            if (_refreshTimer == null)
            {
                _refreshTimer = new Timer(
                    async _ => await RefreshTokenAsync(),
                    null,
                    TimeSpan.FromMinutes(1),
                    Timeout.InfiniteTimeSpan);
            }
            else
            {
                _refreshTimer.Change(TimeSpan.FromMinutes(1), Timeout.InfiniteTimeSpan);
            }
            throw;
        }
    }
    public void Dispose()
    {
        _refreshTimer?.Dispose();
    }
}