using BlazorKompovNet.Models;

namespace BlazorKompovNet.Services.Api;

internal static class ApiMapper
{
    public static Club ToClub(ApiClub source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Address = source.Address,
        IsActive = source.IsActive
    };

    public static Client ToClient(ApiClient source) => new()
    {
        Id = source.Id,
        FirstName = source.FirstName,
        LastName = source.LastName,
        PhoneNumber = source.PhoneNumber,
        Email = source.Email,
        Login = source.Login,
        Password = source.Password,
        RegisteredAt = source.RegisteredAt,
        Balance = source.Balance,
        IsActive = source.IsActive
    };

    public static Cashier ToCashier(ApiCashier source) => new()
    {
        Id = source.Id,
        UserName = source.UserName,
        FullName = source.FullName,
        Password = source.Password,
        Role = source.Role.ToString(),
        IsActive = source.IsActive,
        CreatedAt = source.CreatedAt,
        LastLoginAt = source.LastLoginAt
    };

    public static CashierShift ToShift(ApiCashierShift source, Club? club = null) => new()
    {
        Id = source.Id,
        ClubId = source.ClubId,
        Club = club,
        CashierId = source.CashierId,
        Cashier = source.Cashier is null ? null : ToCashier(source.Cashier),
        OpenedAt = source.OpenedAt,
        ClosedAt = source.ClosedAt,
        OpeningCashAmount = source.OpeningCashAmount,
        CurrentCashAmount = source.CurrentCashAmount
    };

    public static ComputerStatus ToComputerStatus(ApiComputerStatus source) => new()
    {
        Id = source.Id,
        Code = source.Code,
        Name = source.Name,
        CanStartSession = source.CanStartSession
    };

    public static Computer ToComputer(
        ApiComputer source,
        ComputerZone? zone = null,
        IReadOnlyList<ApiComputerStatus>? statuses = null)
    {
        var status = source.Status is null ? null : ToComputerStatus(source.Status);
        if (status is null && statuses is not null)
        {
            var matched = statuses.FirstOrDefault(item => item.Id == source.ComputerStatusId);
            if (matched is not null)
            {
                status = ToComputerStatus(matched);
            }
        }

        var computer = new Computer
        {
            Id = source.Id,
            ClubId = source.ClubId,
            Number = source.Number,
            Name = source.Name ?? source.Number,
            ComputerStatusId = source.ComputerStatusId,
            Status = status,
            ZoneId = source.ZoneId,
            Zone = zone,
            Processor = source.Processor,
            GraphicsCard = source.GraphicsCard,
            RamGb = source.RamGb,
            Monitor = source.Monitor
        };

        if (zone is not null)
        {
            ApplyZoneHardware(computer, zone);
        }

        return computer;
    }

    public static void ApplyZoneHardware(Computer computer, ComputerZone zone)
    {
        if (string.IsNullOrWhiteSpace(computer.Processor) && !string.IsNullOrWhiteSpace(zone.CPU))
        {
            computer.Processor = zone.CPU.Trim();
        }

        if (string.IsNullOrWhiteSpace(computer.GraphicsCard) && !string.IsNullOrWhiteSpace(zone.GPU))
        {
            computer.GraphicsCard = zone.GPU.Trim();
        }

        if (string.IsNullOrWhiteSpace(computer.Monitor))
        {
            computer.Monitor = BuildZoneMonitorLabel(zone);
        }
    }

    private static string? BuildZoneMonitorLabel(ComputerZone zone)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(zone.MonitorSize))
        {
            parts.Add(zone.MonitorSize.Trim());
        }

        if (!string.IsNullOrWhiteSpace(zone.MonitorResolution))
        {
            parts.Add(zone.MonitorResolution.Trim());
        }

        if (!string.IsNullOrWhiteSpace(zone.MonitorHz))
        {
            parts.Add($"{zone.MonitorHz.Trim()} Гц");
        }

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }

    public static ComputerZone ToZone(ApiComputerZone source, Club? club = null)
    {
        var zone = new ComputerZone
        {
            Id = source.Id,
            ClubId = source.ClubId,
            Club = club,
            Name = source.Name,
            Description = source.Description,
            CPU = source.CPU,
            GPU = source.GPU,
            RAM = source.RAM,
            MonitorResolution = source.MonitorResolution,
            MonitorHz = source.MonitorHz,
            MonitorSize = source.MonitorSize
        };

        zone.Computers = source.Computers
            .Select(computer => ToComputer(computer, zone))
            .ToList();

        return zone;
    }

    public static TariffZone ToTariffZone(ApiTariffZone source) => new()
    {
        Id = source.Id,
        TariffId = source.TariffId,
        ZoneId = source.ZoneId,
        Price = source.Price,
        IsActive = source.IsActive
    };

    public static Tariff ToTariff(ApiTariff source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Description = source.Description,
        Duration = source.Duration,
        IsHourly = source.IsHourly,
        IsActive = source.IsActive,
        TariffZones = source.TariffZones.Select(ToTariffZone).ToList()
    };

    public static PaymentType ToPaymentType(ApiPaymentType source) => new()
    {
        Id = source.Id,
        Code = source.Code,
        Name = source.Name,
        IsBonus = source.IsBonus,
        IsActive = source.IsActive
    };

    public static Booking ToBooking(ApiBooking source, Club? club = null) => new()
    {
        Id = source.Id,
        ClubId = source.ClubId,
        Club = club,
        ClientId = source.ClientId,
        ComputerId = source.ComputerId,
        ZoneId = source.ZoneId,
        StartsAt = source.StartsAt,
        EndsAt = source.EndsAt,
        Status = source.Status.ToString(),
        CreatedByCashierId = source.CreatedByCashierId
    };

    public static GameSession ToSession(ApiGameSession source, Club? club = null) => new()
    {
        Id = source.Id,
        ClubId = source.ClubId,
        Club = club,
        ComputerId = source.ComputerId,
        ClientId = source.ClientId,
        CashierShiftId = source.CashierShiftId,
        TariffId = source.TariffId,
        TariffZoneId = source.TariffZoneId,
        StartedAt = source.StartedAt,
        PlannedEndAt = source.PlannedEndAt,
        EndedAt = source.EndedAt,
        InitialPrice = source.InitialPrice,
        TotalPrice = source.TotalPrice,
        Status = source.Status.ToString()
    };

    public static Transaction ToTransaction(ApiTransaction source, Club? club = null) => new()
    {
        Id = source.Id,
        ClubId = source.ClubId,
        Club = club,
        CashierShiftId = source.CashierShiftId,
        ClientId = source.ClientId,
        GameSessionId = source.GameSessionId,
        PaymentTypeId = source.PaymentTypeId,
        Amount = source.Amount,
        Type = source.Type.ToString(),
        Status = source.Status.ToString(),
        CreatedAt = source.CreatedAt
    };

    public static ApiClient ToApiClient(Client source) => new()
    {
        Id = source.Id,
        FirstName = source.FirstName,
        LastName = source.LastName,
        PhoneNumber = source.PhoneNumber,
        Email = source.Email,
        Login = source.Login,
        Password = source.Password,
        RegisteredAt = source.RegisteredAt,
        Balance = source.Balance,
        IsActive = source.IsActive
    };

    public static ApiBooking ToApiBooking(Booking source) => new()
    {
        Id = source.Id,
        ClubId = source.ClubId,
        ClientId = source.ClientId,
        ComputerId = source.ComputerId,
        ZoneId = source.ZoneId,
        StartsAt = source.StartsAt,
        EndsAt = source.EndsAt,
        Status = Enum.Parse<ApiBookingStatus>(source.Status),
        CreatedByCashierId = source.CreatedByCashierId
    };

    public static ApiGameSession ToApiSession(GameSession source) => new()
    {
        Id = source.Id,
        ClubId = source.ClubId,
        ComputerId = source.ComputerId,
        ClientId = source.ClientId,
        CashierShiftId = source.CashierShiftId,
        TariffId = source.TariffId,
        TariffZoneId = source.TariffZoneId,
        StartedAt = source.StartedAt,
        PlannedEndAt = source.PlannedEndAt,
        EndedAt = source.EndedAt,
        InitialPrice = source.InitialPrice,
        TotalPrice = source.TotalPrice,
        Status = Enum.Parse<ApiGameSessionStatus>(source.Status)
    };

    public static ApiTransaction ToApiTransaction(Transaction source) => new()
    {
        Id = source.Id,
        ClubId = source.ClubId,
        CashierShiftId = source.CashierShiftId,
        ClientId = source.ClientId,
        GameSessionId = source.GameSessionId,
        PaymentTypeId = source.PaymentTypeId,
        Amount = source.Amount,
        Type = Enum.Parse<ApiTransactionType>(source.Type),
        Status = Enum.Parse<ApiPaymentStatus>(source.Status),
        CreatedAt = source.CreatedAt
    };

    public static ApiCashierShift ToApiShift(CashierShift source) => new()
    {
        Id = source.Id,
        ClubId = source.ClubId,
        CashierId = source.CashierId,
        OpenedAt = source.OpenedAt,
        ClosedAt = source.ClosedAt,
        OpeningCashAmount = source.OpeningCashAmount,
        CurrentCashAmount = source.CurrentCashAmount
    };

    public static ApiComputer ToApiComputer(Computer source) => new()
    {
        Id = source.Id,
        ClubId = source.ClubId,
        Number = source.Number,
        Name = source.Name,
        ComputerStatusId = source.ComputerStatusId,
        ZoneId = source.ZoneId,
        Processor = source.Processor,
        GraphicsCard = source.GraphicsCard,
        RamGb = source.RamGb,
        Monitor = source.Monitor
    };
}
