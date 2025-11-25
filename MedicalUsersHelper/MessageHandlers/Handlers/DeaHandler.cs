using Application.Core.DTOs.DrugEnforcementAdministration.UI;
using Application.Core.Interfaces.DrugEnforcementAdministration;
using Photino.NET;

namespace MedicalUsersHelper.MessageHandlers.Handlers;

public sealed class DeaHandler : BaseMessageHandler
{
    private readonly IDrugEnforcementAdministration _deaService;
    
    public override string Command => "dea";

    public DeaHandler(IDrugEnforcementAdministration deaService)
    {
        _deaService = deaService;
    }

    public override void Handle(PhotinoWindow window, string payload)
    {
        try
        {
            var jsonPayload = ExtractJsonFromPayload(payload);
            HandleRequest<DeaRequest>(window, jsonPayload, ProcessDeaRequest);
        }
        catch (Exception ex)
        {
            SendErrorResponse(window, 0, $"Error processing request: {ex.Message}");
        }
    }

    private async void ProcessDeaRequest(PhotinoWindow window, DeaRequest data)
    {
        if (data.IsNarcotic)
        {
            await HandleNdeaRequestAsync(window, data);
        }
        else
        {
            await HandleDeaRequestAsync(window, data);
        }
    }

    private async Task HandleDeaRequestAsync(PhotinoWindow window, DeaRequest data)
    {
        var result = await _deaService.CreateDrugEnforcementAdministrationNumber(data.LastName);

        if (result.IsSuccess)
        {
            SendSuccessResponse(window, data.RequestId, "deaNumber", 
                result.Value.DrugEnforcementAdministrationNumber);
        }
        else
        {
            SendErrorResponse(window, data.RequestId, result.Error.Message);
        }
    }

    private async Task HandleNdeaRequestAsync(PhotinoWindow window, DeaRequest data)
    {
        var result = await _deaService.CreateNarcoticDrugEnforcementAddictionNumber(data.LastName);

        if (result.IsSuccess)
        {
            SendSuccessResponse(window, data.RequestId, "deaNumber", 
                result.Value.NarcoticDrugEnforcementAddictionNumber);
        }
        else
        {
            SendErrorResponse(window, data.RequestId, result.Error.Message);
        }
    }
}