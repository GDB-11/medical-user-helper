using System.Text.Json;
using MedicalUsersHelper.PhotinoHelpers;
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

    /// <summary>
    /// Deserialize JSON payload and execute an action with error handling
    /// </summary>
    protected void HandleRequest<TRequest>(PhotinoWindow window, string jsonPayload, Action<PhotinoWindow, TRequest> handler)
        where TRequest : class
    {
        try
        {
            var data = JsonSerializer.Deserialize<TRequest>(jsonPayload);
            
            if (data is null)
            {
                window.SendError($"{Command}:response:0", "Invalid request data");
                return;
            }

            handler(window, data);
        }
        catch (Exception ex)
        {
            window.SendError($"{Command}:response:0", $"Error processing request: {ex.Message}");
        }
    }

    /// <summary>
    /// Send a success response with a result value
    /// </summary>
    protected void SendSuccessResponse<T>(PhotinoWindow window, int requestId, string propertyName, T value)
    {
        window.SendJsonMessage($"{Command}:response:{requestId}", new Dictionary<string, object>
        {
            ["success"] = true,
            [propertyName] = value!
        });
    }

    /// <summary>
    /// Send an error response
    /// </summary>
    protected void SendErrorResponse(PhotinoWindow window, int requestId, string errorMessage)
    {
        window.SendJsonMessage($"{Command}:response:{requestId}", new
        {
            success = false,
            error = errorMessage
        });
    }
}