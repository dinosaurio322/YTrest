using MediatR;
using Microsoft.Extensions.Logging;
using YTapi.Application.DTOs.Responses;
using YTapi.Application.Interfaces;
using YTapi.Application.Queries.Spotify;
using YTapi.Domain.Common;

namespace YTapi.Application.Queries.Handlers;

// ============================================================================
// HANDLER 1: Get Track by ID
// ============================================================================
public sealed class GetSpotifyTrackQueryHandler 
    : IRequestHandler<GetSpotifyTrackQuery, Result<SpotifyTrackResponse>>
{
    private readonly ISpotifyService _spotifyService;
    private readonly ILogger<GetSpotifyTrackQueryHandler> _logger;

    public GetSpotifyTrackQueryHandler(
        ISpotifyService spotifyService,
        ILogger<GetSpotifyTrackQueryHandler> logger)
    {
        _spotifyService = spotifyService;
        _logger = logger;
    }

    public async Task<Result<SpotifyTrackResponse>> Handle(
        GetSpotifyTrackQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving Spotify track: {SpotifyId}", request.SpotifyId);

        var result = await _spotifyService.GetTrackAsync(request.SpotifyId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to retrieve track {SpotifyId}: {Error}",
                request.SpotifyId,
                result.Error!.Message);
            
            return Result<SpotifyTrackResponse>.Failure(result.Error);
        }

        var track = result.Value!;
        var response = new SpotifyTrackResponse
        {
            Id = track.Id,
            Name = track.Name,
            DurationMs = track.DurationMs,
            PreviewUrl = track.PreviewUrl,
            Album = track.Album,
            CoverUrl = track.CoverUrl,
            Artists = track.Artists
        };

        return Result<SpotifyTrackResponse>.Success(response);
    }
}

// ============================================================================
// HANDLER 2: Get Album by ID
// ============================================================================
public sealed class GetSpotifyAlbumQueryHandler 
    : IRequestHandler<GetSpotifyAlbumQuery, Result<SpotifyAlbumResponse>>
{
    private readonly ISpotifyService _spotifyService;
    private readonly ILogger<GetSpotifyAlbumQueryHandler> _logger;

    public GetSpotifyAlbumQueryHandler(
        ISpotifyService spotifyService,
        ILogger<GetSpotifyAlbumQueryHandler> logger)
    {
        _spotifyService = spotifyService;
        _logger = logger;
    }

    public async Task<Result<SpotifyAlbumResponse>> Handle(
        GetSpotifyAlbumQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving Spotify album: {SpotifyId}", request.SpotifyId);

        var result = await _spotifyService.GetAlbumAsync(request.SpotifyId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to retrieve album {SpotifyId}: {Error}",
                request.SpotifyId,
                result.Error!.Message);
            
            return Result<SpotifyAlbumResponse>.Failure(result.Error);
        }

        var album = result.Value!;
        var response = new SpotifyAlbumResponse
        {
            Id = album.Id,
            Name = album.Name,
            ReleaseDate = album.ReleaseDate,
            TotalTracks = album.TotalTracks,
            CoverUrl = album.CoverUrl,
            Artists = album.Artists,
            Tracks = album.Tracks.Select(t => new SpotifyTrackResponse
            {
                Id = t.Id,
                Name = t.Name,
                DurationMs = t.DurationMs,
                PreviewUrl = t.PreviewUrl,
                Album = album.Name,
                CoverUrl = t.CoverUrl ?? album.CoverUrl,
                Artists = t.Artists
            }).ToList()
        };

        return Result<SpotifyAlbumResponse>.Success(response);
    }
}

// ============================================================================
// HANDLER 3: Get Artist by ID
// ============================================================================
public sealed class GetSpotifyArtistQueryHandler 
    : IRequestHandler<GetSpotifyArtistQuery, Result<SpotifyArtistResponse>>
{
    private readonly ISpotifyService _spotifyService;
    private readonly ILogger<GetSpotifyArtistQueryHandler> _logger;

    public GetSpotifyArtistQueryHandler(
        ISpotifyService spotifyService,
        ILogger<GetSpotifyArtistQueryHandler> logger)
    {
        _spotifyService = spotifyService;
        _logger = logger;
    }

    public async Task<Result<SpotifyArtistResponse>> Handle(
        GetSpotifyArtistQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving Spotify artist: {SpotifyId}", request.SpotifyId);

        var result = await _spotifyService.GetArtistAsync(request.SpotifyId, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to retrieve artist {SpotifyId}: {Error}",
                request.SpotifyId,
                result.Error!.Message);
            
            return Result<SpotifyArtistResponse>.Failure(result.Error);
        }

        var artist = result.Value!;
        var response = new SpotifyArtistResponse
        {
            Id = artist.Id,
            Name = artist.Name,
            Popularity = artist.Popularity,
            Genres = artist.Genres,
            ImageUrl = artist.ImageUrl
        };

        return Result<SpotifyArtistResponse>.Success(response);
    }
}

// ============================================================================
// HANDLER 4: Search Tracks
// ============================================================================
public sealed class SearchSpotifyTracksQueryHandler 
    : IRequestHandler<SearchSpotifyTracksQuery, Result<IReadOnlyList<SpotifyTrackResponse>>>
{
    private readonly ISpotifyService _spotifyService;
    private readonly ILogger<SearchSpotifyTracksQueryHandler> _logger;

    public SearchSpotifyTracksQueryHandler(
        ISpotifyService spotifyService,
        ILogger<SearchSpotifyTracksQueryHandler> logger)
    {
        _spotifyService = spotifyService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SpotifyTrackResponse>>> Handle(
        SearchSpotifyTracksQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching Spotify tracks: {Query}", request.Query);

        var result = await _spotifyService.SearchTracksAsync(request.Query, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to search tracks for query '{Query}': {Error}",
                request.Query,
                result.Error!.Message);
            
            return Result<IReadOnlyList<SpotifyTrackResponse>>.Failure(result.Error);
        }

        var tracks = result.Value!;
        var responses = tracks.Select(t => new SpotifyTrackResponse
        {
            Id = t.Id,
            Name = t.Name,
            DurationMs = t.DurationMs,
            PreviewUrl = t.PreviewUrl,
            Album = t.Album,
            CoverUrl = t.CoverUrl,
            Artists = t.Artists
        }).ToList().AsReadOnly();

        _logger.LogInformation("Found {Count} tracks for query '{Query}'", responses.Count, request.Query);

        return Result<IReadOnlyList<SpotifyTrackResponse>>.Success(responses);
    }
}

// ============================================================================
// HANDLER 5: Search Albums
// ============================================================================
public sealed class SearchSpotifyAlbumsQueryHandler 
    : IRequestHandler<SearchSpotifyAlbumsQuery, Result<IReadOnlyList<SpotifyAlbumResponse>>>
{
    private readonly ISpotifyService _spotifyService;
    private readonly ILogger<SearchSpotifyAlbumsQueryHandler> _logger;

    public SearchSpotifyAlbumsQueryHandler(
        ISpotifyService spotifyService,
        ILogger<SearchSpotifyAlbumsQueryHandler> logger)
    {
        _spotifyService = spotifyService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SpotifyAlbumResponse>>> Handle(
        SearchSpotifyAlbumsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching Spotify albums: {Query}", request.Query);

        var result = await _spotifyService.SearchAlbumsAsync(request.Query, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to search albums for query '{Query}': {Error}",
                request.Query,
                result.Error!.Message);
            
            return Result<IReadOnlyList<SpotifyAlbumResponse>>.Failure(result.Error);
        }

        var albums = result.Value!;
        var responses = albums.Select(a => new SpotifyAlbumResponse
        {
            Id = a.Id,
            Name = a.Name,
            ReleaseDate = a.ReleaseDate,
            TotalTracks = a.TotalTracks,
            CoverUrl = a.CoverUrl,
            Artists = a.Artists,
            Tracks = a.Tracks.Select(t => new SpotifyTrackResponse
            {
                Id = t.Id,
                Name = t.Name,
                DurationMs = t.DurationMs,
                PreviewUrl = t.PreviewUrl,
                Album = a.Name,
                CoverUrl = t.CoverUrl ?? a.CoverUrl,
                Artists = t.Artists
            }).ToList()
        }).ToList().AsReadOnly();

        _logger.LogInformation("Found {Count} albums for query '{Query}'", responses.Count, request.Query);

        return Result<IReadOnlyList<SpotifyAlbumResponse>>.Success(responses);
    }
}

// ============================================================================
// HANDLER 6: Search Artists
// ============================================================================
public sealed class SearchSpotifyArtistsQueryHandler 
    : IRequestHandler<SearchSpotifyArtistsQuery, Result<IReadOnlyList<SpotifyArtistResponse>>>
{
    private readonly ISpotifyService _spotifyService;
    private readonly ILogger<SearchSpotifyArtistsQueryHandler> _logger;

    public SearchSpotifyArtistsQueryHandler(
        ISpotifyService spotifyService,
        ILogger<SearchSpotifyArtistsQueryHandler> logger)
    {
        _spotifyService = spotifyService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SpotifyArtistResponse>>> Handle(
        SearchSpotifyArtistsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching Spotify artists: {Query}", request.Query);

        var result = await _spotifyService.SearchArtistsAsync(request.Query, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to search artists for query '{Query}': {Error}",
                request.Query,
                result.Error!.Message);
            
            return Result<IReadOnlyList<SpotifyArtistResponse>>.Failure(result.Error);
        }

        var artists = result.Value!;
        var responses = artists.Select(a => new SpotifyArtistResponse
        {
            Id = a.Id,
            Name = a.Name,
            Popularity = a.Popularity,
            Genres = a.Genres,
            ImageUrl = a.ImageUrl
        }).ToList().AsReadOnly();

        _logger.LogInformation("Found {Count} artists for query '{Query}'", responses.Count, request.Query);

        return Result<IReadOnlyList<SpotifyArtistResponse>>.Success(responses);
    }
}

/// <summary>
/// Handler for getting an artist's top tracks.
/// </summary>
public sealed class GetArtistTopTracksQueryHandler 
    : IRequestHandler<GetArtistTopTracksQuery, Result<IReadOnlyList<SpotifyTrackResponse>>>
{
    private readonly ISpotifyService _spotifyService;
    private readonly ILogger<GetArtistTopTracksQueryHandler> _logger;

    public GetArtistTopTracksQueryHandler(
        ISpotifyService spotifyService,
        ILogger<GetArtistTopTracksQueryHandler> logger)
    {
        _spotifyService = spotifyService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SpotifyTrackResponse>>> Handle(
        GetArtistTopTracksQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting top {Limit} tracks for artist: {ArtistId}", 
            request.Limit, 
            request.ArtistId);

        var result = await _spotifyService.GetArtistTopTracksAsync(
            request.ArtistId, 
            request.Limit, 
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "Failed to retrieve top tracks for artist {ArtistId}: {Error}",
                request.ArtistId,
                result.Error!.Message);
            
            return Result<IReadOnlyList<SpotifyTrackResponse>>.Failure(result.Error);
        }

        var tracks = result.Value!;
        var responses = tracks.Select(t => new SpotifyTrackResponse
        {
            Id = t.Id,
            Name = t.Name,
            DurationMs = t.DurationMs,
            PreviewUrl = t.PreviewUrl,
            Album = t.Album,
            CoverUrl = t.CoverUrl,
            Artists = t.Artists
        }).ToList().AsReadOnly();

        _logger.LogInformation(
            "Found {Count} top tracks for artist {ArtistId}", 
            responses.Count, 
            request.ArtistId);

        return Result<IReadOnlyList<SpotifyTrackResponse>>.Success(responses);
    }
}