using MediatR;
using Microsoft.Extensions.Logging;
using YTapi.Application.Commands.Downloads;
using YTapi.Application.DTOs.Responses;
using YTapi.Application.Interfaces;
using YTapi.Domain.Common;
using YTapi.Domain.Entities;
using YTapi.Domain.Enums;

namespace YTapi.Application.Commands.Handlers;

public sealed class DownloadArtistCommandHandler 
    : IRequestHandler<DownloadArtistCommand, Result<DownloadJobResponse>>
{
    private readonly ISpotifyService _spotifyService;
    private readonly IDownloadQueue _downloadQueue;
    private readonly IDownloadJobStore _jobStore;
    private readonly ILogger<DownloadArtistCommandHandler> _logger;

    public DownloadArtistCommandHandler(
        ISpotifyService spotifyService,
        IDownloadQueue downloadQueue,
        IDownloadJobStore jobStore,
        ILogger<DownloadArtistCommandHandler> logger)
    {
        _spotifyService = spotifyService;
        _downloadQueue = downloadQueue;
        _jobStore = jobStore;
        _logger = logger;
    }

    public async Task<Result<DownloadJobResponse>> Handle(
        DownloadArtistCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing download artist command for Spotify ID: {SpotifyId}",
            request.SpotifyId);

        try
        {
            // Obtener información del artista
            var artistResult = await _spotifyService.GetArtistAsync(
                request.SpotifyId, 
                cancellationToken);

            if (artistResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to retrieve artist {SpotifyId}: {Error}",
                    request.SpotifyId,
                    artistResult.Error!.Message);
                
                return Result<DownloadJobResponse>.Failure(artistResult.Error);
            }

            var artist = artistResult.Value!;

            // Obtener top tracks del artista
            var topTracksResult = await _spotifyService.GetArtistTopTracksAsync(
                request.SpotifyId,
                10,
                cancellationToken);

            if (topTracksResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to retrieve top tracks for artist {SpotifyId}: {Error}",
                    request.SpotifyId,
                    topTracksResult.Error!.Message);
                
                return Result<DownloadJobResponse>.Failure(topTracksResult.Error);
            }

            var topTracks = topTracksResult.Value!;

            if (!topTracks.Any())
            {
                return Result<DownloadJobResponse>.Failure(
                    Error.Validation(
                        "Artist.NoTracks", 
                        "The artist does not have any top tracks available."));
            }

            // Crear download job
            var jobResult = DownloadJob.Create(
                SpotifyItemType.Artist,
                topTracks.ToList(),
                request.ChatId);

            if (jobResult.IsFailure)
            {
                return Result<DownloadJobResponse>.Failure(jobResult.Error!);
            }

            var job = jobResult.Value!;

            if (request.MessageId.HasValue)
            {
                job.SetProgressMessageId(request.MessageId.Value);
            }

            // Guardar job y encolar
            await _jobStore.SaveAsync(job, cancellationToken);
            await _downloadQueue.EnqueueAsync(job.Id, cancellationToken);

            _logger.LogInformation(
                "Download job created with ID: {JobId} for artist: {ArtistName} ({TrackCount} tracks)",
                job.Id,
                artist.Name,
                topTracks.Count);

            var response = new DownloadJobResponse
            {
                JobId = job.Id,
                Status = "Queued",
                Message = $"Download job created for artist: {artist.Name} ({topTracks.Count} top tracks)"
            };

            return Result<DownloadJobResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing download artist command");
            return Result<DownloadJobResponse>.Failure(
                Error.Failure(
                    "DownloadArtist.Error", 
                    "An error occurred while creating the download job."));
        }
    }
}