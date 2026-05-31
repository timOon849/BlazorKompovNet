namespace BlazorKompovNet.Services.Api;

public sealed class ApiOptions
{
    public string BaseUrl { get; set; } = "http://10.80.104.157:5232";

    public string AdminPanelPath { get; set; } = "api/admin-panel";

    public string BlazorCrmPath { get; set; } = "api/blazor-crm";

    public string MobileAppPath { get; set; } = "api/mobile-app";
}
