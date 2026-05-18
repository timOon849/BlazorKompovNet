namespace BlazorKompovNet.Models;

public sealed class Client
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public decimal Balance { get; set; }

    public List<Booking> Bookings { get; set; } = [];
}
