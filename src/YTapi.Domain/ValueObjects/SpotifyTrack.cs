namespace YTapi.Domain.ValueObjects;

/// <summary>
/// Represents a Spotify track with its metadata.
/// </summary>
public sealed record SpotifyTrack
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int DurationMs { get; init; }
    public string? PreviewUrl { get; init; }
    public required string Album { get; init; }
    public string? CoverUrl { get; init; }
    public required IReadOnlyList<string> Artists { get; init; }

    public string GetSearchQuery()
    {
        return $"{Name} {string.Join(" ", Artists)} official audio";
    }

    public static SpotifyTrack Create(
        string id,
        string name,
        int durationMs,
        string album,
        IEnumerable<string> artists,
        string? previewUrl = null,
        string? coverUrl = null)
    {
        return new SpotifyTrack
        {
            Id = id,
            Name = name,
            DurationMs = durationMs,
            PreviewUrl = previewUrl,
            Album = album,
            CoverUrl = coverUrl,
            Artists = artists.ToList().AsReadOnly()
        };
    }
}