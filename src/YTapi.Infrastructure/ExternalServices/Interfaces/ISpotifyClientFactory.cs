

using SpotifyAPI.Web;

namespace YTapi.Infrastructure.ExternalServices.Interfaces;

public interface ISpotifyClientFactory
{
    SpotifyClient Client { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
}