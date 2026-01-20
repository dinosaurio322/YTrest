
namespace YTapi.Domain.Exceptions;

/// <summary>
/// Exception thrown when a Spotify resource is not found.
/// </summary>
public sealed class SpotifyResourceNotFoundException : DomainException
{
    public SpotifyResourceNotFoundException(string resourceType, string resourceId)
        : base($"Spotify {resourceType} with ID '{resourceId}' was not found.")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public string ResourceType { get; }
    public string ResourceId { get; }
}