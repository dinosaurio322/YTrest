

namespace YTapi.Domain.Exceptions;

  
/// <summary>
/// Exception thrown when download job is not found.
/// </summary>
public sealed class DownloadJobNotFoundException : DomainException
{
    public DownloadJobNotFoundException(Guid jobId)
        : base($"Download job with ID '{jobId}' was not found.")
    {
        JobId = jobId;
    }

    public Guid JobId { get; }
}