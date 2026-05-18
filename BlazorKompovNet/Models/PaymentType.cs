namespace BlazorKompovNet.Models;

public sealed class PaymentType
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsBonus { get; set; }

    public bool IsActive { get; set; } = true;
}
