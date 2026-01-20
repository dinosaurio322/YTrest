


using MediatR;
using Microsoft.Extensions.Logging;
using YTapi.Application.Commands.Downloads;
using YTapi.Application.DTOs.Responses;
using YTapi.Application.Interfaces;
using YTapi.Domain.Common;
using YTapi.Domain.Entities;
using YTapi.Domain.Enums;

namespace YTapi.Application.Commands.Handlers;

public sealed class DownloadAlbumCommandHandler 
    : IRequestHandler<DownloadAlbumCommand, Result<DownloadJobResponse>>
{
    private readonly ISpotifyService _spotifyService;
    private readonly IDownloadQueue _downloadQueue;
    private readonly IDownloadJobStore _jobStore;
    private readonly ILogger<DownloadAlbumCommandHandler> _logger;

    public DownloadAlbumCommandHandler(
        ISpotifyService spotifyService,
        IDownloadQueue downloadQueue,
        IDownloadJobStore jobStore,
        ILogger<DownloadAlbumCommandHandler> logger)
    {
        _spotifyService = spotifyService;
        _downloadQueue = downloadQueue;
        _jobStore = jobStore;
        _logger = logger;
    }

    public async Task<Result<DownloadJobResponse>> Handle(
        DownloadAlbumCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing download album command for Spotify ID: {SpotifyId}",
            request.SpotifyId);

        try
        {
            // Get album from Spotify
            var albumResult = await _spotifyService.GetAlbumAsync(request.SpotifyId, cancellationToken);
            
            if (albumResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to retrieve album {SpotifyId}: {Error}",
                    request.SpotifyId,
                    albumResult.Error!.Message);
                
                return Result<DownloadJobResponse>.Failure(albumResult.Error);
            }

            var album = albumResult.Value!;

            if (!album.Tracks.Any())
            {
                return Result<DownloadJobResponse>.Failure(
                    Error.Validation("Album.NoTracks", "The album does not contain any tracks."));
            }

            // Create download job
            var jobResult = DownloadJob.Create(SpotifyItemType.Album, album.Tracks);

            if (jobResult.IsFailure)
            {
                return Result<DownloadJobResponse>.Failure(jobResult.Error!);
            }

            var job = jobResult.Value!;

            // Save job and enqueue
            await _jobStore.SaveAsync(job, cancellationToken);
            await _downloadQueue.EnqueueAsync(job.Id, cancellationToken);

            _logger.LogInformation(
                "Download job created with ID: {JobId} for album: {AlbumName} ({TrackCount} tracks)",
                job.Id,
                album.Name,
                album.Tracks.Count);

            var response = new DownloadJobResponse
            {
                JobId = job.Id,
                Status = "Queued",
                Message = $"Download job created for album: {album.Name} ({album.Tracks.Count} tracks)"
            };

            return Result<DownloadJobResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing download album command");
            return Result<DownloadJobResponse>.Failure(
                Error.Failure("DownloadAlbum.Error", "An error occurred while creating the download job."));
        }
    }
}