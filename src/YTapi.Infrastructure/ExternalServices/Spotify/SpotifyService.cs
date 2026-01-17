

using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using YTapi.Application.Interfaces;
using YTapi.Domain.Common;
using YTapi.Domain.Exceptions;
using YTapi.Domain.ValueObjects;
using YTapi.Infrastructure.ExternalServices.Interfaces;

namespace YTapi.Infrastructure.ExternalServices.Spotify;

public sealed class SpotifyService : ISpotifyService
{
    private readonly ISpotifyClientFactory _clientFactory;
    private readonly ILogger<SpotifyService> _logger;

    public SpotifyService(
        ISpotifyClientFactory clientFactory,
        ILogger<SpotifyService> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<Result<SpotifyTrack>> GetTrackAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result<SpotifyTrack>.Failure(
                Error.Validation("Spotify.InvalidId", "Track ID cannot be empty"));
        }

        try
        {
            var track = await _clientFactory.Client.Tracks.Get(id);

            if (track is null)
            {
                return Result<SpotifyTrack>.Failure(
                    Error.NotFound("Spotify.TrackNotFound", $"Track with ID {id} was not found"));
            }

            var spotifyTrack = MapToSpotifyTrack(track);
            return Result<SpotifyTrack>.Success(spotifyTrack);
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Spotify track {TrackId} not found", id);
            return Result<SpotifyTrack>.Failure(
                Error.NotFound("Spotify.TrackNotFound", $"Track with ID {id} was not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Spotify track {TrackId}", id);
            return Result<SpotifyTrack>.Failure(
                Error.Failure("Spotify.Error", "An error occurred while retrieving the track"));
        }
    }

    public async Task<Result<SpotifyAlbum>> GetAlbumAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result<SpotifyAlbum>.Failure(
                Error.Validation("Spotify.InvalidId", "Album ID cannot be empty"));
        }

        try
        {
            var album = await _clientFactory.Client.Albums.Get(id);

            if (album is null)
            {
                return Result<SpotifyAlbum>.Failure(
                    Error.NotFound("Spotify.AlbumNotFound", $"Album with ID {id} was not found"));
            }

            var spotifyAlbum = MapToSpotifyAlbum(album);
            return Result<SpotifyAlbum>.Success(spotifyAlbum);
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Spotify album {AlbumId} not found", id);
            return Result<SpotifyAlbum>.Failure(
                Error.NotFound("Spotify.AlbumNotFound", $"Album with ID {id} was not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Spotify album {AlbumId}", id);
            return Result<SpotifyAlbum>.Failure(
                Error.Failure("Spotify.Error", "An error occurred while retrieving the album"));
        }
    }

    public async Task<Result<SpotifyArtist>> GetArtistAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result<SpotifyArtist>.Failure(
                Error.Validation("Spotify.InvalidId", "Artist ID cannot be empty"));
        }

        try
        {
            var artist = await _clientFactory.Client.Artists.Get(id);

            if (artist is null)
            {
                return Result<SpotifyArtist>.Failure(
                    Error.NotFound("Spotify.ArtistNotFound", $"Artist with ID {id} was not found"));
            }

            var spotifyArtist = MapToSpotifyArtist(artist);
            return Result<SpotifyArtist>.Success(spotifyArtist);
        }
        catch (APIException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Spotify artist {ArtistId} not found", id);
            return Result<SpotifyArtist>.Failure(
                Error.NotFound("Spotify.ArtistNotFound", $"Artist with ID {id} was not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Spotify artist {ArtistId}", id);
            return Result<SpotifyArtist>.Failure(
                Error.Failure("Spotify.Error", "An error occurred while retrieving the artist"));
        }
    }

    public async Task<Result<IReadOnlyList<SpotifyTrack>>> SearchTracksAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Result<IReadOnlyList<SpotifyTrack>>.Failure(
                Error.Validation("Spotify.InvalidQuery", "Search query cannot be empty"));
        }

        try
        {
            var searchRequest = new SearchRequest(SearchRequest.Types.Track, query);
            var result = await _clientFactory.Client.Search.Item(searchRequest);

            var tracks = result.Tracks?.Items?
                .Where(t => t != null)
                .Select(MapToSpotifyTrack)
                .ToList()
                .AsReadOnly() ?? new List<SpotifyTrack>().AsReadOnly();

            return Result<IReadOnlyList<SpotifyTrack>>.Success(tracks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Spotify tracks with query: {Query}", query);
            return Result<IReadOnlyList<SpotifyTrack>>.Failure(
                Error.Failure("Spotify.SearchError", "An error occurred while searching for tracks"));
        }
    }

    public async Task<Result<IReadOnlyList<SpotifyAlbum>>> SearchAlbumsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Result<IReadOnlyList<SpotifyAlbum>>.Failure(
                Error.Validation("Spotify.InvalidQuery", "Search query cannot be empty"));
        }

        try
        {
            var searchRequest = new SearchRequest(SearchRequest.Types.Album, query);
            var result = await _clientFactory.Client.Search.Item(searchRequest);

            var albums = result.Albums?.Items?
                .Where(a => a != null)
                .Select(MapToSpotifyAlbum)
                .ToList()
                .AsReadOnly() ?? new List<SpotifyAlbum>().AsReadOnly();

            return Result<IReadOnlyList<SpotifyAlbum>>.Success(albums);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Spotify albums with query: {Query}", query);
            return Result<IReadOnlyList<SpotifyAlbum>>.Failure(
                Error.Failure("Spotify.SearchError", "An error occurred while searching for albums"));
        }
    }

    public async Task<Result<IReadOnlyList<SpotifyArtist>>> SearchArtistsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Result<IReadOnlyList<SpotifyArtist>>.Failure(
                Error.Validation("Spotify.InvalidQuery", "Search query cannot be empty"));
        }

        try
        {
            var searchRequest = new SearchRequest(SearchRequest.Types.Artist, query);
            var result = await _clientFactory.Client.Search.Item(searchRequest);

            var artists = result.Artists?.Items?
                .Where(a => a != null)
                .Select(MapToSpotifyArtist)
                .ToList()
                .AsReadOnly() ?? new List<SpotifyArtist>().AsReadOnly();

            return Result<IReadOnlyList<SpotifyArtist>>.Success(artists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Spotify artists with query: {Query}", query);
            return Result<IReadOnlyList<SpotifyArtist>>.Failure(
                Error.Failure("Spotify.SearchError", "An error occurred while searching for artists"));
        }
    }
    public async Task<Result<IReadOnlyList<SpotifyTrack>>> GetArtistTopTracksAsync(
        string artistId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
         
        try
        {
            var topTracks = await _clientFactory.Client.Artists.GetTopTracks(
                artistId,
                new ArtistsTopTracksRequest("US"));

            if (topTracks?.Tracks == null || !topTracks.Tracks.Any())
            {
                _logger.LogWarning("No top tracks found for artist: {ArtistId}", artistId);
                return Result<IReadOnlyList<SpotifyTrack>>.Success(
                    Array.Empty<SpotifyTrack>().ToList().AsReadOnly());
            }

            var tracks = topTracks.Tracks
                .Take(Math.Min(limit, 10))
                .Select(MapToSpotifyTrack)
                .ToList()
                .AsReadOnly();

            _logger.LogInformation(
                "Retrieved {Count} top tracks for artist: {ArtistId}",
                tracks.Count,
                artistId);

            return Result<IReadOnlyList<SpotifyTrack>>.Success(tracks);
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top tracks for artist: {ArtistId}", artistId);
            return Result<IReadOnlyList<SpotifyTrack>>.Failure(
                Error.Failure("Spotify.Error", "An error occurred while retrieving artist top tracks"));
        }
    }
    private static SpotifyTrack MapToSpotifyTrack(FullTrack track)
    {
        var coverUrl = track.Album?.Images?
            .OrderByDescending(i => i.Width)
            .FirstOrDefault()
            ?.Url;

        return SpotifyTrack.Create(
            track.Id,
            track.Name,
            track.DurationMs,
            track.Album?.Name ?? "Unknown Album",
            track.Artists.Select(a => a.Name),
            track.PreviewUrl,
            coverUrl);
    }

    private static SpotifyAlbum MapToSpotifyAlbum(FullAlbum album)
    {
        var coverUrl = album.Images?
            .OrderByDescending(i => i.Width)
            .FirstOrDefault()
            ?.Url;

        return new SpotifyAlbum
        {
            Id = album.Id,
            Name = album.Name,
            ReleaseDate = album.ReleaseDate,
            TotalTracks = album.TotalTracks,
            CoverUrl = coverUrl,
            Artists = album.Artists.Select(a => a.Name).ToList().AsReadOnly(),
            Tracks = album.Tracks.Items.Select(t => SpotifyTrack.Create(
                t.Id,
                t.Name,
                t.DurationMs,
                album.Name,
                t.Artists.Select(a => a.Name),
                coverUrl: coverUrl
            )).ToList().AsReadOnly()
        };
    }

    private static SpotifyAlbum MapToSpotifyAlbum(SimpleAlbum album)
    {
        var coverUrl = album.Images?
            .OrderByDescending(i => i.Width)
            .FirstOrDefault()
            ?.Url;

        return new SpotifyAlbum
        {
            Id = album.Id,
            Name = album.Name,
            ReleaseDate = album.ReleaseDate,
            TotalTracks = album.TotalTracks,
            CoverUrl = coverUrl,
            Artists = album.Artists.Select(a => a.Name).ToList().AsReadOnly(),
            Tracks = new List<SpotifyTrack>().AsReadOnly()
        };
    }

    private static SpotifyArtist MapToSpotifyArtist(FullArtist artist)
    {
        var imageUrl = artist.Images?
            .OrderByDescending(i => i.Width)
            .FirstOrDefault()
            ?.Url;

        return new SpotifyArtist
        {
            Id = artist.Id,
            Name = artist.Name,
            Popularity = artist.Popularity,
            Genres = artist.Genres.ToList().AsReadOnly(),
            ImageUrl = imageUrl
        };
    }
}