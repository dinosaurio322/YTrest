using System.Collections.Concurrent;
using YTapi.Application.Interfaces;
using YTapi.Domain.Common;
using YTapi.Domain.Entities;

namespace YTapi.Infrastructure.BackgroundJobs;

/// <summary>
/// In-memory implementation of download job store.
/// Stores both job metadata and result streams in memory.
/// For production with multiple instances, replace with Redis or database.
/// </summary>
public sealed class InMemoryDownloadJobStore : IDownloadJobStore
{
    private readonly ConcurrentDictionary<Guid, DownloadJob> _jobs = new();
    private readonly ConcurrentDictionary<Guid, Stream> _resultStreams = new();

    /// <summary>
    /// Saves a new download job.
    /// </summary>
    public Task SaveAsync(DownloadJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        _jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves a download job by ID.
    /// </summary>
    public Task<DownloadJob?> GetAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    /// <summary>
    /// Updates an existing download job.
    /// </summary>
    public Task UpdateAsync(DownloadJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        _jobs[job.Id] = job;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Saves the result stream for a completed download job.
    /// </summary>
    public Task SaveResultStreamAsync(Guid jobId, Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Create a copy of the stream to store
        var memoryStream = new MemoryStream();
        stream.Position = 0;
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        _resultStreams[jobId] = memoryStream;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves the result stream for a download job.
    /// </summary>
    public Task<Result<Stream>> GetResultStreamAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (_resultStreams.TryGetValue(jobId, out var stream))
        {
            // Return a new stream positioned at the beginning
            stream.Position = 0;
            return Task.FromResult(Result<Stream>.Success(stream));
        }

        return Task.FromResult(Result<Stream>.Failure(
            Error.NotFound(
                "ResultStream.NotFound",
                $"Result stream for job {jobId} was not found. The job may not be completed yet.")));
    }

    /// <summary>
    /// Deletes a job and its result stream (for cleanup).
    /// </summary>
    public Task DeleteAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _jobs.TryRemove(jobId, out _);
        
        if (_resultStreams.TryRemove(jobId, out var stream))
        {
            stream?.Dispose();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the total number of jobs stored.
    /// </summary>
    public int JobCount => _jobs.Count;

    /// <summary>
    /// Gets the total number of result streams stored.
    /// </summary>
    public int ResultStreamCount => _resultStreams.Count;

    /// <summary>
    /// Clears all jobs and result streams (for testing/cleanup).
    /// </summary>
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _jobs.Clear();

        foreach (var stream in _resultStreams.Values)
        {
            stream?.Dispose();
        }
        _resultStreams.Clear();

        return Task.CompletedTask;
    }
}