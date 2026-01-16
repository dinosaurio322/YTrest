
namespace YTapi.Application.DTOs.Requests;

public sealed record SearchSpotifyRequest
{
    public required string Query { get; init; }
    public string Type { get; init; } = "track"; // track, album, artist
}