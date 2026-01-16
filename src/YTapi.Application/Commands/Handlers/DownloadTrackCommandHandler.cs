

using MediatR;
using Microsoft.Extensions.Logging;
using YTapi.Application.Commands.Downloads;
using YTapi.Application.DTOs.Responses;
using YTapi.Application.Interfaces;
using YTapi.Domain.Common;
using YTapi.Domain.Entities;
using YTapi.Domain.Enums;
using YTapi.Domain.Exceptions;

namespace YTapi.Application.Commands.Handlers;

public sealed class DownloadTrackCommandHandler 
    : IRequestHandler<DownloadTrackCommand, Result<DownloadJobResponse>>
{
    private readonly ISpotifyService _spotifyService;
    private readonly IDownloadQueue _downloadQueue;
    private readonly IDownloadJobStore _jobStore;
    private readonly ILogger<DownloadTrackCommandHandler> _logger;

    public DownloadTrackCommandHandler(
        ISpotifyService spotifyService,
        IDownloadQueue downloadQueue,
        IDownloadJobStore jobStore,
        ILogger<DownloadTrackCommandHandler> logger)
    {
        _spotifyService = spotifyService;
        _downloadQueue = downloadQueue;
        _jobStore = jobStore;
        _logger = logger;
    }

    public async Task<Result<DownloadJobResponse>> Handle(
        DownloadTrackCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing download track command for Spotify ID: {SpotifyId}",
            request.SpotifyId);

        try
        {
            // Get track from Spotify
            var trackResult = await _spotifyService.GetTrackAsync(request.SpotifyId, cancellationToken);
            
            if (trackResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to retrieve track {SpotifyId}: {Error}",
                    request.SpotifyId,
                    trackResult.Error!.Message);
                
                return Result<DownloadJobResponse>.Failure(trackResult.Error);
            }

            // Create download job
            var jobResult = DownloadJob.Create(
                SpotifyItemType.Track,
                new[] { trackResult.Value! });

            if (jobResult.IsFailure)
            {
                return Result<DownloadJobResponse>.Failure(jobResult.Error!);
            }

            var job = jobResult.Value!;

            // Save job and enqueue
            await _jobStore.SaveAsync(job, cancellationToken);
            await _downloadQueue.EnqueueAsync(job.Id, cancellationToken);

            _logger.LogInformation(
                "Download job created with ID: {JobId} for track: {TrackName}",
                job.Id,
                trackResult.Value.Name);

            var response = new DownloadJobResponse
            {
                JobId = job.Id,
                Status = "Queued",
                Message = $"Download job created for track: {trackResult.Value.Name}"
            };

            return Result<DownloadJobResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing download track command");
            return Result<DownloadJobResponse>.Failure(
                Error.Failure("DownloadTrack.Error", "An error occurred while creating the download job."));
        }
    }
}
 