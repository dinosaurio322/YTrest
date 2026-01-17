using MediatR;
using Microsoft.AspNetCore.Mvc;
using YTapi.Application.Commands.Downloads;
using YTapi.Application.DTOs.Requests;
using YTapi.Application.DTOs.Responses;
using YTapi.Application.Queries.Downloads;
using YTapi.Domain.Common;

namespace YTapi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DownloadsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DownloadsController> _logger;

    public DownloadsController(
        IMediator mediator,
        ILogger<DownloadsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Download a single track from Spotify
    /// </summary>
    /// <param name="request">Track download request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Download job information</returns>
    [HttpPost("track")]
    [ProducesResponseType(typeof(DownloadJobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadTrack(
        [FromBody] DownloadTrackRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Download track request received for Spotify ID: {SpotifyId}", request.SpotifyId);

        var command = new DownloadTrackCommand(request.SpotifyId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                Domain.Common.ErrorType.NotFound => NotFound(new ProblemDetails
                {
                    Title = "Track Not Found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound
                }),
                Domain.Common.ErrorType.Validation => BadRequest(new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest
                }),
                _ => BadRequest(new ProblemDetails
                {
                    Title = "Error",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest
                })
            };
        }

        return Accepted(result.Value);
    }

    /// <summary>
    /// Download an entire album from Spotify
    /// </summary>
    /// <param name="request">Album download request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Download job information</returns>
    [HttpPost("album")]
    [ProducesResponseType(typeof(DownloadJobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAlbum(
        [FromBody] DownloadAlbumRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Download album request received for Spotify ID: {SpotifyId}", request.SpotifyId);

        var command = new DownloadAlbumCommand(request.SpotifyId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                Domain.Common.ErrorType.NotFound => NotFound(new ProblemDetails
                {
                    Title = "Album Not Found",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status404NotFound
                }),
                Domain.Common.ErrorType.Validation => BadRequest(new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest
                }),
                _ => BadRequest(new ProblemDetails
                {
                    Title = "Error",
                    Detail = result.Error.Message,
                    Status = StatusCodes.Status400BadRequest
                })
            };
        }

        return Accepted(result.Value);
    }



    /// <summary>
    /// Download an artist's top tracks (ZIP file).
    /// </summary>
    /// <param name="request">Artist download request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Download job information</returns>
    /// <response code="202">Job created successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Artist not found</response>
    [HttpPost("artist")]
    [ProducesResponseType(typeof(DownloadJobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadArtist(
        [FromBody] DownloadArtistRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Download artist request received for Spotify ID: {SpotifyId}",
            request.SpotifyId);

        var command = new DownloadArtistCommand(request.SpotifyId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            var error = result.Error!;

            return error.Type switch
            {
                ErrorType.NotFound => NotFound(new ProblemDetails
                {
                    Title = "Artist Not Found",
                    Detail = error.Message,
                    Status = StatusCodes.Status404NotFound
                }),
                ErrorType.Validation => BadRequest(new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = error.Message,
                    Status = StatusCodes.Status400BadRequest
                }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Download Failed",
                    Detail = error.Message,
                    Status = StatusCodes.Status500InternalServerError
                })
            };
        }

        return Accepted(result.Value);
    }

    /// <summary>
    /// Get the status of a download job
    /// </summary>
    /// <param name="jobId">Download job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Download job status</returns>
    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(DownloadStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDownloadStatus(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var query = new GetDownloadStatusQuery(jobId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Job Not Found",
                Detail = result.Error!.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Download the completed file
    /// </summary>
    /// <param name="jobId">Download job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Audio file (MP3 or ZIP)</returns>
    [HttpGet("{jobId:guid}/file")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DownloadFile(
        Guid jobId,
        CancellationToken cancellationToken)

    {
        _logger.LogInformation("Download file request for job: {JobId}", jobId);

        // First check job status
        var statusQuery = new GetDownloadStatusQuery(jobId);
        var statusResult = await _mediator.Send(statusQuery, cancellationToken);

        if (statusResult.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Job Not Found",
                Detail = statusResult.Error!.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        var status = statusResult.Value!;

        if (status.Status != "Completed")
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Job Not Completed",
                Detail = $"Job is in status: {status.Status}. Cannot download file yet.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Get the file
        var fileQuery = new DownloadFileQuery(jobId);
        var fileResult = await _mediator.Send(fileQuery, cancellationToken);

        if (fileResult.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "File Not Found",
                Detail = fileResult.Error!.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        var contentType = status.TotalTracks > 1 ? "application/zip" : "audio/mpeg";
        var fileName = status.TotalTracks > 1 ? $"album_{jobId}.zip" : $"track_{jobId}.mp3";

        return File(fileResult.Value!, contentType, fileName);
    }

}