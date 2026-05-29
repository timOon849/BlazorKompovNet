namespace BlazorKompovNet.Services.Api;

public sealed class ApiOptions
{
    public string BaseUrl { get; set; } = "http://127.0.0.1:5232";

    public string AdminPanelPath { get; set; } = "api/admin-panel";

    public string BlazorCrmPath { get; set; } = "api/blazor-crm";

    public string MobileAppPath { get; set; } = "api/mobile-app";
}
