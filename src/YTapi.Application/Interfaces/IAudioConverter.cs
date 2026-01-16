


using YTapi.Domain.Common;
using YTapi.Domain.ValueObjects;

namespace YTapi.Application.Interfaces;
/// <summary>
/// Service for converting audio formats.
/// </summary>
public interface IAudioConverter
{
    Task<Result<Stream>> ConvertToMp3Async(
        Stream input,
        SpotifyTrack metadata,
        IProgress<double> progress,
        CancellationToken cancellationToken = default);
}