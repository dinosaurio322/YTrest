
namespace YTapi.Application.Interfaces;



/// <summary>
/// Queue for managing download jobs.
/// </summary>
public interface IDownloadQueue
{
    Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<Guid> DequeueAsync(CancellationToken cancellationToken = default);
}
