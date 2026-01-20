
namespace YTapi.Application.DTOs.Requests;

/// <summary>
/// Request to download an artist's top tracks.
/// </summary>
public sealed class DownloadArtistRequest
{
    /// <summary>
    /// Spotify artist ID (22 characters).
    /// </summary>
    public string SpotifyId { get; set; } = string.Empty;

    /// <summary>
    /// Number of top tracks to download (max 10, default 10).
    /// </summary>
    public int Limit { get; set; } = 10;
}