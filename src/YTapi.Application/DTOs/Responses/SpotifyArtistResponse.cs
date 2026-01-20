

namespace YTapi.Application.DTOs.Responses;

public sealed record SpotifyArtistResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int Popularity { get; init; }
    public required IReadOnlyList<string> Genres { get; init; }
    public string? ImageUrl { get; init; }
}