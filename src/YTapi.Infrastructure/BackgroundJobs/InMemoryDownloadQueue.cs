using System.Collections.Concurrent;
using YTapi.Application.Interfaces;

namespace YTapi.Infrastructure.BackgroundJobs;

/// <summary>
/// In-memory implementation of download queue using concurrent data structures.
/// Thread-safe and suitable for single-instance deployments.
/// For distributed systems, replace with Redis, RabbitMQ, or similar.
/// </summary>
public sealed class InMemoryDownloadQueue : IDownloadQueue
{
    private readonly ConcurrentQueue<Guid> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    /// <summary>
    /// Enqueues a download job ID for processing.
    /// </summary>
    public Task EnqueueAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(jobId);
        _signal.Release(); // Signal that an item is available
        return Task.CompletedTask;
    }

    /// <summary>
    /// Dequeues the next download job ID, waiting if queue is empty.
    /// </summary>
    public async Task<Guid> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await _signal.WaitAsync(cancellationToken);
        
        if (_queue.TryDequeue(out var jobId))
        {
            return jobId;
        }

        // This should never happen due to semaphore coordination
        throw new InvalidOperationException("Failed to dequeue job despite signal.");
    }

    /// <summary>
    /// Gets the current number of jobs in the queue.
    /// </summary>
    public int Count => _queue.Count;
}