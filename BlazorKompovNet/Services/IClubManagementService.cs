using BlazorKompovNet.Models;

namespace BlazorKompovNet.Services;

public interface IClubManagementService
{
    Task<IReadOnlyList<ComputerZone>> GetZonesAsync();

    Task<IReadOnlyList<Tariff>> GetTariffsAsync();

    Task<IReadOnlyList<Client>> GetClientsAsync();

    Task<Client?> GetClientAsync(int clientId);

    Task<IReadOnlyList<Client>> SearchClientsAsync(string? searchText);

    Task<IReadOnlyList<GameSession>> GetActiveSessionsAsync();

    Task<IReadOnlyList<Booking>> GetActiveBookingsAsync();

    Task<IReadOnlyList<Booking>> GetBookingsAsync();

    Task<Booking?> GetBookingAsync(int bookingId);

    Task<IReadOnlyList<GameSession>> GetSessionsAsync();

    Task<GameSession?> GetSessionAsync(int sessionId);

    Task<IReadOnlyList<SessionExtension>> GetSessionExtensionsAsync(int sessionId);

    Task<IReadOnlyList<Transaction>> GetTransactionsAsync();

    Task<Transaction?> GetTransactionAsync(int transactionId);

    Task<IReadOnlyList<CashierShift>> GetCashierShiftsAsync();

    Task<CashierShift?> GetCurrentShiftAsync();

    Task<IReadOnlyList<PaymentType>> GetPaymentTypesAsync();

    Task<ClubOperationResult> RegisterClientAsync(string firstName, string lastName, string? phoneNumber, string? email);

    Task<ClubOperationResult> OpenShiftAsync(int cashierId, decimal openingCashAmount);

    Task<ClubOperationResult> CloseShiftAsync(int shiftId, decimal closingCashAmount);

    Task<ClubOperationResult> TopUpClientBalanceAsync(int clientId, decimal amount, int paymentTypeId);

    Task<ClubOperationResult> CreateBookingAsync(int clientId, int computerId, DateTime startsAt, DateTime endsAt);

    Task<ClubOperationResult> CancelBookingAsync(int bookingId);

    Task<ClubOperationResult> MarkBookingNoShowAsync(int bookingId);

    Task<ClubOperationResult> StartSessionAsync(int computerId, int clientId, int tariffId, decimal hours);

    Task<ClubOperationResult> ExtendSessionByTariffAsync(int sessionId, int tariffId, decimal hours);

    Task<ClubOperationResult> AddSessionTimeAsync(int sessionId, decimal hours, string reason);

    Task<ClubOperationResult> CompleteSessionAsync(int sessionId);

    Task<ClubOperationResult> TurnOnComputerAsync(int computerId);

    Task<ClubOperationResult> TurnOffComputerAsync(int computerId);

    Task<ClubOperationResult> RestartComputerAsync(int computerId);
}
