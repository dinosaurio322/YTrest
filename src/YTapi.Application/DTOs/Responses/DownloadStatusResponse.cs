

namespace YTapi.Application.DTOs.Responses;


public sealed record DownloadStatusResponse
{
    public required Guid JobId { get; init; }
    public required string Status { get; init; }
    public required double Progress { get; init; }
    public string? CurrentTrackName { get; init; }
    public int CompletedTracks { get; init; }
    public int TotalTracks { get; init; }
    public string? ErrorMessage { get; init; }
}