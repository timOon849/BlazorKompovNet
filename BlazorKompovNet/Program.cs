using System.Net;
using BlazorKompovNet.Components;
using BlazorKompovNet.Services;
using BlazorKompovNet.Services.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "BlazorKompovNet.Auth";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));

var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://127.0.0.1:5232";
builder.Services.AddHttpClient<KompovApiClient>((_, client) =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestVersion = HttpVersion.Version11;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    UseProxy = false,
    PooledConnectionLifetime = TimeSpan.FromMinutes(1),
    ConnectTimeout = TimeSpan.FromSeconds(15),
    AutomaticDecompression = DecompressionMethods.All
});

builder.Services.AddScoped<ICashierRepository, ApiCashierRepository>();
builder.Services.AddScoped<IClubManagementService, ApiClubManagementService>();
builder.Services.AddScoped<IDashboardService, ApiDashboardService>();

var app = builder.Build();

app.Logger.LogInformation("KompovNet API: {ApiBaseUrl}", apiBaseUrl);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/auth/login", async (HttpContext httpContext, ICashierRepository cashiers) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var userName = form["userName"].ToString();
    var password = form["password"].ToString();

    try
    {
        var cashier = await cashiers.ValidateCredentialsAsync(userName, password);
        if (cashier is null)
        {
            return Results.Redirect("/login?error=1");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, cashier.Id.ToString()),
            new(ClaimTypes.Name, cashier.FullName),
            new(ClaimTypes.Role, cashier.Role.ToString()),
            new("UserName", cashier.UserName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        await cashiers.UpdateLastLoginAsync(cashier.Id);

        return Results.Redirect("/");
    }
    catch (InvalidOperationException)
    {
        return Results.Redirect("/login?error=2");
    }
}).DisableAntiforgery();

app.MapPost("/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
