
namespace YTapi.Application.Interfaces;


/// <summary>
/// Processor for handling download jobs.
/// </summary>
public interface IDownloadProcessor
{
    Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default);
}
