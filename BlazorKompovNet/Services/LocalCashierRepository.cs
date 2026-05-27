using BlazorKompovNet.Models;

namespace BlazorKompovNet.Services;

public sealed class LocalCashierRepository : ICashierRepository
{
    private readonly List<Cashier> cashiers;

    public LocalCashierRepository()
    {
        cashiers =
        [
            new()
            {
                Id = 1,
                UserName = "admin",
                FullName = "Главный кассир",
                Password = "admin",
                Role = CashierRole.Administrator
            }
        ];
    }

    public Task<Cashier?> ValidateCredentialsAsync(string userName, string password)
    {
        var cashier = cashiers.FirstOrDefault(cashier =>
            cashier.IsActive &&
            string.Equals(cashier.UserName, userName, StringComparison.OrdinalIgnoreCase));

        if (cashier is null || cashier.Password != password)
        {
            return Task.FromResult<Cashier?>(null);
        }

        return Task.FromResult<Cashier?>(cashier);
    }

    public Task UpdateLastLoginAsync(int cashierId)
    {
        var cashier = cashiers.FirstOrDefault(cashier => cashier.Id == cashierId);
        if (cashier is not null)
        {
            cashier.LastLoginAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task<Cashier?> GetByIdAsync(int cashierId)
    {
        var cashier = cashiers.FirstOrDefault(cashier => cashier.Id == cashierId && cashier.IsActive);
        return Task.FromResult(cashier);
    }
}
