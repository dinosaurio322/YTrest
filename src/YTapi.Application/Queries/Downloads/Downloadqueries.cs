
using MediatR;
using YTapi.Application.DTOs.Responses;
using YTapi.Domain.Common;

namespace YTapi.Application.Queries.Downloads;

public sealed record GetDownloadStatusQuery(Guid JobId) 
    : IRequest<Result<DownloadStatusResponse>>;

public sealed record DownloadFileQuery(Guid JobId) 
    : IRequest<Result<Stream>>;