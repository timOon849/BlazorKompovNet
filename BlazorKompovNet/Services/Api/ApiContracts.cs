namespace BlazorKompovNet.Services.Api;

public enum ApiBookingStatus
{
    Created,
    Active,
    Completed,
    Cancelled,
    NoShow
}

public enum ApiGameSessionStatus
{
    Active,
    Completed
}

public enum ApiTransactionType
{
    BalanceTopUp,
    BonusAccrual,
    SessionStart,
    SessionExtension
}

public enum ApiPaymentStatus
{
    Pending,
    Paid,
    Refunded,
    Cancelled
}

public enum ApiCashierRole
{
    Cashier,
    Administrator
}

public sealed class ApiClub
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ApiClient
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Login { get; set; }
    public string? Password { get; set; }
    public DateTime RegisteredAt { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ApiCashier
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public ApiCashierRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public sealed class ApiCashierShift
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public int CashierId { get; set; }
    public ApiCashier? Cashier { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCashAmount { get; set; }
    public decimal CurrentCashAmount { get; set; }
}

public sealed class ApiComputerStatus
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool CanStartSession { get; set; }
}

public sealed class ApiComputer
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int ComputerStatusId { get; set; }
    public ApiComputerStatus? Status { get; set; }
    public int ZoneId { get; set; }
    public string? Processor { get; set; }
    public string? GraphicsCard { get; set; }
    public int RamGb { get; set; }
    public string? Monitor { get; set; }
}

public sealed class ApiComputerZone
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CPU { get; set; } = string.Empty;
    public string GPU { get; set; } = string.Empty;
    public string RAM { get; set; } = string.Empty;
    public string MonitorResolution { get; set; } = string.Empty;
    public string MonitorHz { get; set; } = string.Empty;
    public string MonitorSize { get; set; } = string.Empty;
    public List<ApiComputer> Computers { get; set; } = [];
}

public sealed class ApiTariff
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsHourly { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ApiTariffZone> TariffZones { get; set; } = [];
}

public sealed class ApiTariffZone
{
    public int Id { get; set; }
    public int TariffId { get; set; }
    public int ZoneId { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ApiPaymentType
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsBonus { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ApiBooking
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public int ClientId { get; set; }
    public int ComputerId { get; set; }
    public int ZoneId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public ApiBookingStatus Status { get; set; }
    public int? CreatedByCashierId { get; set; }
}

public sealed class ApiGameSession
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public int ComputerId { get; set; }
    public int ClientId { get; set; }
    public int CashierShiftId { get; set; }
    public int TariffId { get; set; }
    public int TariffZoneId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime PlannedEndAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public decimal InitialPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public ApiGameSessionStatus Status { get; set; }
}

public sealed class ApiTransaction
{
    public int Id { get; set; }
    public int ClubId { get; set; }
    public int? CashierShiftId { get; set; }
    public int? ClientId { get; set; }
    public int? GameSessionId { get; set; }
    public int PaymentTypeId { get; set; }
    public decimal Amount { get; set; }
    public ApiTransactionType Type { get; set; }
    public ApiPaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ApiAdminPanelContext
{
    public int? DefaultClubId { get; set; }
    public List<ApiClub> Clubs { get; set; } = [];
    public List<ApiComputerStatus> ComputerStatuses { get; set; } = [];
    public List<ApiPaymentType> PaymentTypes { get; set; } = [];
}

public sealed class ApiDashboardStats
{
    public ApiClub? CurrentClub { get; set; }
    public List<ApiClub> AvailableClubs { get; set; } = [];
    public decimal RevenueToday { get; set; }
    public decimal RevenueCurrentShift { get; set; }
    public int VisitorsToday { get; set; }
    public int ActiveVisitors { get; set; }
    public int AvailableComputers { get; set; }
    public int BusyComputers { get; set; }
    public int ReservedComputers { get; set; }
    public int MaintenanceComputers { get; set; }
    public int DisabledComputers { get; set; }
    public int BookingsToday { get; set; }
    public int ActiveSessionsCount { get; set; }
    public int CompletedSessionsToday { get; set; }
    public int TopUpsToday { get; set; }
    public decimal TopUpAmountToday { get; set; }
    public ApiCashierShift? CurrentShift { get; set; }
}
