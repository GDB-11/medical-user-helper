using Photino.NET;

namespace MedicalUsersHelper.MessageHandlers;

/// <summary>
/// Base class for message handlers with common functionality
/// </summary>
public abstract class BaseMessageHandler : IMessageHandler
{
    public abstract string Command { get; }
    public abstract void Handle(PhotinoWindow window, string payload);

    /// <summary>
    /// Extract JSON from payload that may be in "request:id:json" format
    /// </summary>
    protected static string ExtractJsonFromPayload(string payload)
    {        
        if (string.IsNullOrWhiteSpace(payload))
        {
            return payload;
        }

        // Check if it starts with a JSON character
        var trimmed = payload.TrimStart();
        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
        {
            // Already JSON
            return payload;
        }
        
        // Find the second colon (after "request:id:")
        var firstColon = payload.IndexOf(':');
        if (firstColon == -1)
        {
            // No colon, assume it's already JSON
            return payload;
        }

        var secondColon = payload.IndexOf(':', firstColon + 1);
        if (secondColon == -1)
        {
            // Only one colon, assume it's already JSON after first colon
            return payload[(firstColon + 1)..];
        }

        // Return everything after the second colon
        return payload[(secondColon + 1)..];
    }
}