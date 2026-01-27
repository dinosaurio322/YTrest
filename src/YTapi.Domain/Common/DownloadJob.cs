using YTapi.Domain.Common;
using YTapi.Domain.Enums;
using YTapi.Domain.ValueObjects;

namespace YTapi.Domain.Entities;

/// <summary>
/// Represents a download job in the system.
/// </summary>
public sealed class DownloadJob : Entity
{
    private readonly List<SpotifyTrack> _tracks = new();

    private DownloadJob(
        SpotifyItemType itemType,
        IEnumerable<SpotifyTrack> tracks,
        long chatId) : base()
    {
        ItemType = itemType;
        _tracks.AddRange(tracks);
        ChatId = chatId;
        Status = DownloadStatus.Pending;
        Progress = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public SpotifyItemType ItemType { get; private set; }
    public IReadOnlyList<SpotifyTrack> Tracks => _tracks.AsReadOnly();
    public long ChatId { get; private set; }
    public DownloadStatus Status { get; private set; }
    public double Progress { get; private set; }
    public string? CurrentTrackName { get; private set; }
    public int CompletedTracks { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? ProgressMessageId { get; private set; }

    public void SetProgressMessageId(int messageId) => ProgressMessageId = messageId;

    public static Result<DownloadJob> Create(
        SpotifyItemType itemType,
        IEnumerable<SpotifyTrack> tracks,
        long chatId = 0)
    {
        if (!tracks.Any())
            return Result<DownloadJob>.Failure(
                Error.Validation("DownloadJob.NoTracks", "At least one track is required."));

        var job = new DownloadJob(itemType, tracks, chatId);
        return Result<DownloadJob>.Success(job);
    }

    public void StartProcessing()
    {
        if (Status != DownloadStatus.Pending)
            return;

        Status = DownloadStatus.Processing;
    }

    public void UpdateProgress(double progress, string? currentTrackName = null)
    {
        Progress = Math.Clamp(progress, 0, 100);
        CurrentTrackName = currentTrackName;
    }

    public void CompleteTrack()
    {
        CompletedTracks++;
        Progress = (double)CompletedTracks / Tracks.Count * 100;
    }

    public void Complete()
    {
        Status = DownloadStatus.Completed;
        Progress = 100;
        CompletedAt = DateTime.UtcNow;
        CurrentTrackName = null;
    }

    public void Fail(string errorMessage)
    {
        Status = DownloadStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    public bool IsCompleted => Status == DownloadStatus.Completed;
    public bool IsFailed => Status == DownloadStatus.Failed;
    public bool IsProcessing => Status == DownloadStatus.Processing;
}