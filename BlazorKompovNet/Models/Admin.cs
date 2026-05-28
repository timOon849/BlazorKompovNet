namespace BlazorKompovNet.Models;

public sealed class Admin
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AdminRole Role { get; set; } = AdminRole.Regular;
}
