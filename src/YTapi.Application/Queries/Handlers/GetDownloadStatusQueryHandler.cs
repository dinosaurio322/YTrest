

using MediatR;
using Microsoft.Extensions.Logging;
using YTapi.Application.DTOs.Responses;
using YTapi.Application.Interfaces;
using YTapi.Application.Queries.Downloads;
using YTapi.Domain.Common;
using YTapi.Domain.Exceptions;

namespace YTapi.Application.Queries.Handlers;

public sealed class GetDownloadStatusQueryHandler 
    : IRequestHandler<GetDownloadStatusQuery, Result<DownloadStatusResponse>>
{
    private readonly IDownloadJobStore _jobStore;
    private readonly ILogger<GetDownloadStatusQueryHandler> _logger;

    public GetDownloadStatusQueryHandler(
        IDownloadJobStore jobStore,
        ILogger<GetDownloadStatusQueryHandler> logger)
    {
        _jobStore = jobStore;
        _logger = logger;
    }

    public async Task<Result<DownloadStatusResponse>> Handle(
        GetDownloadStatusQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving status for job {JobId}", request.JobId);

        var job = await _jobStore.GetAsync(request.JobId, cancellationToken);

        if (job is null)
        {
            _logger.LogWarning("Job {JobId} not found", request.JobId);
            return Result<DownloadStatusResponse>.Failure(
                Error.NotFound("Job.NotFound", $"Job with ID {request.JobId} was not found."));
        }

        var response = new DownloadStatusResponse
        {
            JobId = job.Id,
            Status = job.Status.ToString(),
            Progress = job.Progress,
            CurrentTrackName = job.CurrentTrackName,
            CompletedTracks = job.CompletedTracks,
            TotalTracks = job.Tracks.Count,
            ErrorMessage = job.ErrorMessage
        };

        return Result<DownloadStatusResponse>.Success(response);
    }
}
 