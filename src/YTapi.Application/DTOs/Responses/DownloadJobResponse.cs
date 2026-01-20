namespace YTapi.Application.DTOs.Responses;

public sealed record DownloadJobResponse
{
    public required Guid JobId { get; init; }
    public required string Status { get; init; }
    public required string Message { get; init; }
}
 
 
  
 