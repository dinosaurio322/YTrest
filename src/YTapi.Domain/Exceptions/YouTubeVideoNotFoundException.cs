namespace YTapi.Domain.Exceptions;

/// <summary>
/// Exception thrown when a YouTube video cannot be found.
/// </summary>
public sealed class YouTubeVideoNotFoundException : DomainException
{
    public YouTubeVideoNotFoundException(string query)
        : base($"No YouTube video found for query: '{query}'")
    {
        Query = query;
    }

    public string Query { get; }
}
