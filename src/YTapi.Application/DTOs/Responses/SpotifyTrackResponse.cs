


namespace YTapi.Application.DTOs.Responses;

public sealed record SpotifyTrackResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int DurationMs { get; init; }
    public string? PreviewUrl { get; init; }
    public required string Album { get; init; }
    public string? CoverUrl { get; init; }
    public required IReadOnlyList<string> Artists { get; init; }
}
