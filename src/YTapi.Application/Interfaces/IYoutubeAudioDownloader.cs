


using YTapi.Domain.Common;
using YTapi.Domain.ValueObjects;

namespace YTapi.Application.Interfaces;
/// <summary>
/// Service for downloading audio from YouTube.
/// </summary>
public interface IYoutubeAudioDownloader
{
    Task<Result<Stream>> DownloadAsync(
        string query,
        SpotifyTrack metadata,
        IProgress<double> progress,
        CancellationToken cancellationToken = default);
}
