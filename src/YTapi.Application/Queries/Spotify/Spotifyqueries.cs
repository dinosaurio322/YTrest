using MediatR;
using YTapi.Application.DTOs.Responses;
using YTapi.Domain.Common;

namespace YTapi.Application.Queries.Spotify;

public sealed record GetSpotifyTrackQuery(string SpotifyId) 
    : IRequest<Result<SpotifyTrackResponse>>;

public sealed record GetSpotifyAlbumQuery(string SpotifyId) 
    : IRequest<Result<SpotifyAlbumResponse>>;

public sealed record GetSpotifyArtistQuery(string SpotifyId) 
    : IRequest<Result<SpotifyArtistResponse>>;

public sealed record SearchSpotifyTracksQuery(string Query) 
    : IRequest<Result<IReadOnlyList<SpotifyTrackResponse>>>;

public sealed record SearchSpotifyAlbumsQuery(string Query) 
    : IRequest<Result<IReadOnlyList<SpotifyAlbumResponse>>>;

public sealed record SearchSpotifyArtistsQuery(string Query) 
    : IRequest<Result<IReadOnlyList<SpotifyArtistResponse>>>;

public sealed record GetArtistTopTracksQuery(string ArtistId, int Limit = 10) 
    : IRequest<Result<IReadOnlyList<SpotifyTrackResponse>>>;