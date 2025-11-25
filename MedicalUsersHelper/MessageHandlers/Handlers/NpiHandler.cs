using System.Text.Json;
using Application.Core.DTOs.NationalProviderIdentifier.UI;
using Application.Core.Interfaces.NationalProviderIdentifier;
using Photino.NET;

namespace MedicalUsersHelper.MessageHandlers.Handlers;

public sealed class NpiHandler : BaseMessageHandler
{
    private readonly INationalProviderIdentifier _npiService;
    
    public override string Command => "npi";

    public NpiHandler(INationalProviderIdentifier npiService)
    {
        _npiService = npiService;
    }

    public override void Handle(PhotinoWindow window, string payload)
    {
        try
        {
            var jsonPayload = ExtractJsonFromPayload(payload);
            
            using var jsonDoc = JsonDocument.Parse(jsonPayload);
            var root = jsonDoc.RootElement;
            
            if (!root.TryGetProperty("action", out var actionProp))
            {
                SendErrorResponse(window, 0, "Missing action property");
                return;
            }

            var action = actionProp.GetString();
            
            switch (action)
            {
                case "validate":
                    HandleRequest<NpiValidateRequest>(window, jsonPayload, ProcessValidateRequest);
                    break;
                case "generate":
                    HandleRequest<NpiGenerateRequest>(window, jsonPayload, ProcessGenerateRequest);
                    break;
                default:
                    SendErrorResponse(window, 0, "Unknown action");
                    break;
            }
        }
        catch (Exception ex)
        {
            SendErrorResponse(window, 0, $"Error processing request: {ex.Message}");
        }
    }

    private async void ProcessGenerateRequest(PhotinoWindow window, NpiGenerateRequest data)
    {
        var result = await _npiService.CreateNationalProviderIdentifier(data.IsOrganization);

        if (result.IsSuccess)
        {
            SendSuccessResponse(window, data.RequestId, "npi", 
                result.Value.NationalProviderIdentifier);
        }
        else
        {
            SendErrorResponse(window, data.RequestId, result.Error.Message);
        }
    }

    private void ProcessValidateRequest(PhotinoWindow window, NpiValidateRequest data)
    {
        var result = _npiService.ValidateNpi(data.Npi);

        if (result.IsSuccess)
        {
            SendSuccessResponse(window, data.RequestId, "isValid", result.Value.isValid);
        }
        else
        {
            SendErrorResponse(window, data.RequestId, result.Error.Message);
        }
    }
}