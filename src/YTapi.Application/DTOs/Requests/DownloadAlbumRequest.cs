

namespace YTapi.Application.DTOs.Requests;

public sealed record DownloadAlbumRequest
{
    public required string SpotifyId { get; init; }
}
