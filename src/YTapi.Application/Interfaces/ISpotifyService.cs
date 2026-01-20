using YTapi.Domain.Common;
using YTapi.Domain.Entities;
using YTapi.Domain.ValueObjects;

namespace YTapi.Application.Interfaces;

/// <summary>
/// Service for interacting with Spotify API.
/// </summary>
public interface ISpotifyService
{
    Task<Result<SpotifyTrack>> GetTrackAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<SpotifyAlbum>> GetAlbumAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<SpotifyArtist>> GetArtistAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SpotifyTrack>>> SearchTracksAsync(string query, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SpotifyAlbum>>> SearchAlbumsAsync(string query, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SpotifyArtist>>> SearchArtistsAsync(string query, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<SpotifyTrack>>> GetArtistTopTracksAsync(
        string artistId, 
        int limit = 10, 
        CancellationToken cancellationToken = default);
}
 
  
 
// Additional value objects for Spotify responses
public sealed record SpotifyAlbum
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ReleaseDate { get; init; }
    public required int TotalTracks { get; init; }
    public string? CoverUrl { get; init; }
    public required IReadOnlyList<string> Artists { get; init; }
    public required IReadOnlyList<SpotifyTrack> Tracks { get; init; }
}

public sealed record SpotifyArtist
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int Popularity { get; init; }
    public required IReadOnlyList<string> Genres { get; init; }
    public string? ImageUrl { get; init; }
}