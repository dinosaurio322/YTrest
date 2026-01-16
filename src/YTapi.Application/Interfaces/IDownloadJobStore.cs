



using YTapi.Domain.Common;
using YTapi.Domain.Entities;

namespace YTapi.Application.Interfaces;
 
/// <summary>
/// Store for download jobs and their results.
/// </summary>
public interface IDownloadJobStore
{
    Task SaveAsync(DownloadJob job, CancellationToken cancellationToken = default);
    Task<DownloadJob?> GetAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task UpdateAsync(DownloadJob job, CancellationToken cancellationToken = default);
    Task SaveResultStreamAsync(Guid jobId, Stream stream, CancellationToken cancellationToken = default);
    Task<Result<Stream>> GetResultStreamAsync(Guid jobId, CancellationToken cancellationToken = default);
}