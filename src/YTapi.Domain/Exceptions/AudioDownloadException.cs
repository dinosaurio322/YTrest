

namespace YTapi.Domain.Exceptions;

/// <summary>
/// Exception thrown when audio download fails.
/// </summary>
public sealed class AudioDownloadException : DomainException
{
    public AudioDownloadException(string message, Exception? innerException = null)
        : base(message, innerException!)
    {
    }
}