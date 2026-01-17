using MediatR;
using Microsoft.AspNetCore.Mvc;
using YTapi.Application.DTOs.Requests;
using YTapi.Application.DTOs.Responses;
using YTapi.Application.Queries.Spotify;

namespace YTapi.Api.Controllers;

/// <summary>
/// Controller for Spotify search and browse operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SpotifyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SpotifyController> _logger;

    public SpotifyController(
        IMediator mediator,
        ILogger<SpotifyController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get a track by Spotify ID.
    /// </summary>
    /// <param name="id">Spotify track ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Track information</returns>
    [HttpGet("tracks/{id}")]
    [ProducesResponseType(typeof(SpotifyTrackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTrack(
        string id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get track request for ID: {TrackId}", id);

        var query = new GetSpotifyTrackQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Track Not Found",
                Detail = result.Error!.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get an album by Spotify ID.
    /// </summary>
    /// <param name="id">Spotify album ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Album information with tracks</returns>
    [HttpGet("albums/{id}")]
    [ProducesResponseType(typeof(SpotifyAlbumResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlbum(
        string id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get album request for ID: {AlbumId}", id);

        var query = new GetSpotifyAlbumQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Album Not Found",
                Detail = result.Error!.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get an artist by Spotify ID.
    /// </summary>
    /// <param name="id">Spotify artist ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Artist information</returns>
    [HttpGet("artists/{id}")]
    [ProducesResponseType(typeof(SpotifyArtistResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArtist(
        string id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Get artist request for ID: {ArtistId}", id);

        var query = new GetSpotifyArtistQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Artist Not Found",
                Detail = result.Error!.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(result.Value);
    }
    /// <summary>
    /// Get an artist's top tracks.
    /// </summary>
    /// <param name="id">Spotify artist ID</param>
    /// <param name="limit">Number of tracks to return (max 10)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of top tracks</returns>
    /// <response code="200">Top tracks retrieved successfully</response>
    /// <response code="404">Artist not found</response>
    [HttpGet("artists/{id}/top-tracks")]
    [ProducesResponseType(typeof(IReadOnlyList<SpotifyTrackResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArtistTopTracks(
        string id,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetArtistTopTracksQuery(id, Math.Clamp(limit, 1, 10));
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Artist Top Tracks Not Found",
                Detail = result.Error!.Message,
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(result.Value);
    }
    /// <summary>
    /// Search for tracks on Spotify.
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching tracks</returns>
    [HttpGet("search/tracks")]
    [ProducesResponseType(typeof(IReadOnlyList<SpotifyTrackResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchTracks(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Query",
                Detail = "Search query cannot be empty",
                Status = StatusCodes.Status400BadRequest
            });
        }

        _logger.LogInformation("Search tracks request: {Query}", q);

        var query = new SearchSpotifyTracksQuery(q);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Search Failed",
                Detail = result.Error!.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Search for albums on Spotify.
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching albums</returns>
    [HttpGet("search/albums")]
    [ProducesResponseType(typeof(IReadOnlyList<SpotifyAlbumResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchAlbums(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Query",
                Detail = "Search query cannot be empty",
                Status = StatusCodes.Status400BadRequest
            });
        }

        _logger.LogInformation("Search albums request: {Query}", q);

        var query = new SearchSpotifyAlbumsQuery(q);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Search Failed",
                Detail = result.Error!.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Search for artists on Spotify.
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching artists</returns>
    [HttpGet("search/artists")]
    [ProducesResponseType(typeof(IReadOnlyList<SpotifyArtistResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchArtists(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Query",
                Detail = "Search query cannot be empty",
                Status = StatusCodes.Status400BadRequest
            });
        }

        _logger.LogInformation("Search artists request: {Query}", q);

        var query = new SearchSpotifyArtistsQuery(q);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Search Failed",
                Detail = result.Error!.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(result.Value);
    }


}