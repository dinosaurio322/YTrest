using MediatR;
using Microsoft.Extensions.Logging;
using YTapi.Application.Interfaces;
using YTapi.Application.Queries.Downloads;
using YTapi.Domain.Common;

namespace YTapi.Application.Queries.Handlers;

public sealed class DownloadFileQueryHandler 
    : IRequestHandler<DownloadFileQuery, Result<Stream>>
{
    private readonly IDownloadJobStore _jobStore;
    private readonly ILogger<DownloadFileQueryHandler> _logger;

    public DownloadFileQueryHandler(
        IDownloadJobStore jobStore,
        ILogger<DownloadFileQueryHandler> logger)
    {
        _jobStore = jobStore;
        _logger = logger;
    }

    public async Task<Result<Stream>> Handle(
        DownloadFileQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving download file for job {JobId}", request.JobId);

        var streamResult = await _jobStore.GetResultStreamAsync(request.JobId, cancellationToken);
        
        if (streamResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to get stream for job {JobId}: {Error}", 
                request.JobId, 
                streamResult.Error!.Message);
            
            return streamResult;
        }

        return streamResult;
    }
}