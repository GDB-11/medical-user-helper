using System.Text.Json;
using Application.Core.DTOs.License.UI;
using Application.Core.Interfaces.License;
using Infrastructure.Core.DTOs.License;
using Photino.NET;

namespace MedicalUsersHelper.MessageHandlers.Handlers;

public sealed class LicenseHandler : BaseMessageHandler
{
    private readonly ILicenseNumber _licenseService;
    
    public override string Command => "license";

    public LicenseHandler(ILicenseNumber licenseService)
    {
        _licenseService = licenseService;
    }

    public override void Handle(PhotinoWindow window, string payload)
    {
        try
        {
            var jsonPayload = ExtractJsonFromPayload(payload);
            HandleRequest<LicenseRequest>(window, jsonPayload, ProcessLicenseRequest);
        }
        catch (Exception ex)
        {
            SendErrorResponse(window, 0, $"Error processing request: {ex.Message}");
        }
    }

    private async void ProcessLicenseRequest(PhotinoWindow window, LicenseRequest data)
    {
        var licenseType = data.LicenseType.ToLowerInvariant() == "pharmacy" 
            ? LicenseNumberType.Pharmacy 
            : LicenseNumberType.Medical;

        var result = await _licenseService.CreateLicenseNumber(
            data.StateCode, 
            data.LastName, 
            licenseType
        );

        if (result.IsSuccess)
        {
            SendSuccessResponse(window, data.RequestId, "licenseNumber", 
                result.Value.LicenseNumber);
        }
        else
        {
            SendErrorResponse(window, data.RequestId, result.Error.Message);
        }
    }
}