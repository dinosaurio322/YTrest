


namespace YTapi.Application.DTOs.Responses;

public sealed record SpotifyAlbumResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ReleaseDate { get; init; }
    public required int TotalTracks { get; init; }
    public string? CoverUrl { get; init; }
    public required IReadOnlyList<string> Artists { get; init; }
    public required IReadOnlyList<SpotifyTrackResponse> Tracks { get; init; }
}