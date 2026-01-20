using FluentValidation;
using YTapi.Application.Queries.Spotify;

namespace YTapi.Application.Validators;

/// <summary>
/// Validator for GetSpotifyTrackQuery.
/// </summary>
public sealed class GetSpotifyTrackQueryValidator : AbstractValidator<GetSpotifyTrackQuery>
{
    public GetSpotifyTrackQueryValidator()
    {
        RuleFor(x => x.SpotifyId)
            .NotEmpty()
            .WithMessage("Spotify track ID is required")
            .Length(22)
            .WithMessage("Spotify ID must be exactly 22 characters")
            .Matches("^[a-zA-Z0-9]+$")
            .WithMessage("Spotify ID must contain only alphanumeric characters");
    }
}

/// <summary>
/// Validator for GetSpotifyAlbumQuery.
/// </summary>
public sealed class GetSpotifyAlbumQueryValidator : AbstractValidator<GetSpotifyAlbumQuery>
{
    public GetSpotifyAlbumQueryValidator()
    {
        RuleFor(x => x.SpotifyId)
            .NotEmpty()
            .WithMessage("Spotify album ID is required")
            .Length(22)
            .WithMessage("Spotify ID must be exactly 22 characters")
            .Matches("^[a-zA-Z0-9]+$")
            .WithMessage("Spotify ID must contain only alphanumeric characters");
    }
}

/// <summary>
/// Validator for GetSpotifyArtistQuery.
/// </summary>
public sealed class GetSpotifyArtistQueryValidator : AbstractValidator<GetSpotifyArtistQuery>
{
    public GetSpotifyArtistQueryValidator()
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
/// Validator for SearchSpotifyTracksQuery.
/// </summary>
public sealed class SearchSpotifyTracksQueryValidator : AbstractValidator<SearchSpotifyTracksQuery>
{
    public SearchSpotifyTracksQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .WithMessage("Search query is required")
            .MinimumLength(2)
            .WithMessage("Search query must be at least 2 characters")
            .MaximumLength(100)
            .WithMessage("Search query must not exceed 100 characters");
    }
}

/// <summary>
/// Validator for SearchSpotifyAlbumsQuery.
/// </summary>
public sealed class SearchSpotifyAlbumsQueryValidator : AbstractValidator<SearchSpotifyAlbumsQuery>
{
    public SearchSpotifyAlbumsQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .WithMessage("Search query is required")
            .MinimumLength(2)
            .WithMessage("Search query must be at least 2 characters")
            .MaximumLength(100)
            .WithMessage("Search query must not exceed 100 characters");
    }
}

/// <summary>
/// Validator for SearchSpotifyArtistsQuery.
/// </summary>
public sealed class SearchSpotifyArtistsQueryValidator : AbstractValidator<SearchSpotifyArtistsQuery>
{
    public SearchSpotifyArtistsQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .WithMessage("Search query is required")
            .MinimumLength(2)
            .WithMessage("Search query must be at least 2 characters")
            .MaximumLength(100)
            .WithMessage("Search query must not exceed 100 characters");
    }
}
/// <summary>
/// Validator for GetArtistTopTracksQuery.
/// </summary>
public sealed class GetArtistTopTracksQueryValidator : AbstractValidator<GetArtistTopTracksQuery>
{
    public GetArtistTopTracksQueryValidator()
    {
        RuleFor(x => x.ArtistId)
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