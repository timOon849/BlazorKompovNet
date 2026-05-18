namespace BlazorKompovNet.Models;

public sealed class Booking
{
    public int Id { get; set; }

    public int ClubId { get; set; }

    public Club? Club { get; set; }

    public int ClientId { get; set; }

    public Client? Client { get; set; }

    public int ComputerId { get; set; }

    public Computer? Computer { get; set; }

    public int ZoneId { get; set; }

    public ComputerZone? Zone { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Created;

    public int? CreatedByCashierId { get; set; }

    public Cashier? CreatedByCashier { get; set; }
}
