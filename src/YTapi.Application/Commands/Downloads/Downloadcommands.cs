

using MediatR;
using YTapi.Application.DTOs.Responses;
using YTapi.Domain.Common;

namespace YTapi.Application.Commands.Downloads;

public sealed record DownloadTrackCommand(string SpotifyId) 
    : IRequest<Result<DownloadJobResponse>>;

public sealed record DownloadAlbumCommand(string SpotifyId) 
    : IRequest<Result<DownloadJobResponse>>;

public sealed record DownloadArtistCommand(string SpotifyId) 
    : IRequest<Result<DownloadJobResponse>>;