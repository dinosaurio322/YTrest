


namespace YTapi.Application.DTOs.Responses;

public sealed record DownloadProgressDto
{
    public required Guid JobId { get; init; }
    public required string Status { get; init; }
    public required double Percentage { get; init; }
}