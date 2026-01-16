


namespace YTapi.Domain.Exceptions;


/// <summary>
/// Exception thrown when audio conversion fails.
/// </summary>
public sealed class AudioConversionException : DomainException
{
    public AudioConversionException(string message, Exception? innerException = null)
        : base(message, innerException!)
    {
    }
}
 