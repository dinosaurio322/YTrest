using FluentValidation;
using YTapi.Application.Commands.Downloads;
using YTapi.Application.DTOs.Requests;
using YTapi.Application.Queries.Downloads;

namespace YTapi.Application.Validators;

/// <summary>
/// Validator for DownloadTrackCommand.
/// Ensures the Spotify track ID is valid.
/// </summary>
public sealed class DownloadTrackCommandValidator : AbstractValidator<DownloadTrackCommand>
{
    public DownloadTrackCommandValidator()
    {
        RuleFor(x => x.SpotifyId)
            .NotEmpty()
            .WithMessage("Spotify ID is required")
            .Length(22)
            .WithMessage("Spotify ID must be exactly 22 characters")
            .Matches("^[a-zA-Z0-9]+$")
            .WithMessage("Spotify ID must contain only alphanumeric characters");
    }
}

/// <summary>
/// Validator for DownloadAlbumCommand.
/// Ensures the Spotify album ID is valid.
/// </summary>
public sealed class DownloadAlbumCommandValidator : AbstractValidator<DownloadAlbumCommand>
{
    public DownloadAlbumCommandValidator()
    {
        RuleFor(x => x.SpotifyId)
            .NotEmpty()
            .WithMessage("Spotify ID is required")
            .Length(22)
            .WithMessage("Spotify ID must be exactly 22 characters")
            .Matches("^[a-zA-Z0-9]+$")
            .WithMessage("Spotify ID must contain only alphanumeric characters");
    }
}

/// <summary>
/// Validator for DownloadTrackRequest DTO.
/// </summary>
public sealed class DownloadTrackRequestValidator : AbstractValidator<DownloadTrackRequest>
{
    public DownloadTrackRequestValidator()
    {
        RuleFor(x => x.SpotifyId)
            .NotEmpty()
            .WithMessage("Spotify ID is required")
            .Length(22)
            .WithMessage("Spotify ID must be exactly 22 characters")
            .Matches("^[a-zA-Z0-9]+$")
            .WithMessage("Spotify ID must contain only alphanumeric characters");
    }
}

/// <summary>
/// Validator for DownloadAlbumRequest DTO.
/// </summary>
public sealed class DownloadAlbumRequestValidator : AbstractValidator<DownloadAlbumRequest>
{
    public DownloadAlbumRequestValidator()
    {
        RuleFor(x => x.SpotifyId)
            .NotEmpty()
            .WithMessage("Spotify ID is required")
            .Length(22)
            .WithMessage("Spotify ID must be exactly 22 characters")
            .Matches("^[a-zA-Z0-9]+$")
            .WithMessage("Spotify ID must contain only alphanumeric characters");
    }
}

/// <summary>
/// Validator for GetDownloadStatusQuery.
/// </summary>
public sealed class GetDownloadStatusQueryValidator : AbstractValidator<GetDownloadStatusQuery>
{
    public GetDownloadStatusQueryValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty()
            .WithMessage("Job ID is required")
            .Must(id => id != Guid.Empty)
            .WithMessage("Job ID must be a valid GUID");
    }
}

/// <summary>
/// Validator for DownloadFileQuery.
/// </summary>
public sealed class DownloadFileQueryValidator : AbstractValidator<DownloadFileQuery>
{
    public DownloadFileQueryValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty()
            .WithMessage("Job ID is required")
            .Must(id => id != Guid.Empty)
            .WithMessage("Job ID must be a valid GUID");
    }
}

/// <summary>
/// Validator for SearchSpotifyRequest.
/// </summary>
public sealed class SearchSpotifyRequestValidator : AbstractValidator<SearchSpotifyRequest>
{
    public SearchSpotifyRequestValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .WithMessage("Search query is required")
            .MinimumLength(2)
            .WithMessage("Search query must be at least 2 characters")
            .MaximumLength(100)
            .WithMessage("Search query must not exceed 100 characters");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Search type is required")
            .Must(type => new[] { "track", "album", "artist" }.Contains(type.ToLower()))
            .WithMessage("Search type must be 'track', 'album', or 'artist'");
    }
}
/// <summary>
/// Validator for DownloadArtistCommand.
/// </summary>
public sealed class DownloadArtistCommandValidator : AbstractValidator<DownloadArtistCommand>
{
    public DownloadArtistCommandValidator()
    {
        RuleFor(x => x.SpotifyId)
            .NotEmpty()
            .WithMessage("Spotify artist ID is required")
            .Length(22)
            .WithMessage("Spotify ID must be exactly 22 characters")
            .Matches("^[a-zA-Z0-9]+$")
            .WithMessage("Spotify ID must contain only alphanumeric characters");
    }
}

/// <summary>
/// Validator for DownloadArtistRequest DTO.
/// </summary>
public sealed class DownloadArtistRequestValidator : AbstractValidator<DownloadArtistRequest>
{
    public DownloadArtistRequestValidator()
    {
        RuleFor(x => x.SpotifyId)
            .NotEmpty()
            .WithMessage("Spotify artist ID is required")
            .Length(22)
            .WithMessage("Spotify ID must be exactly 22 characters")
            .Matches("^[a-zA-Z0-9]+$")
            .WithMessage("Spotify ID must contain only alphanumeric characters");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 10)
            .WithMessage("Limit must be between 1 and 10");
    }
}