namespace YTapi.Application.DTOs.Requests;

public sealed record DownloadTrackRequest
{
    public required string SpotifyId { get; init; }
}
 
 