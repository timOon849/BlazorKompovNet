using BlazorKompovNet.Models;
using BlazorKompovNet.Services.Api;

namespace BlazorKompovNet.Services;

public sealed class ApiCashierRepository(KompovApiClient api) : ICashierRepository
{
    public async Task<Cashier?> ValidateCredentialsAsync(string userName, string password)
    {
        var cashiers = await api.GetListAsync<ApiCashier>(api.Admin("cashiers"));
        var match = cashiers.FirstOrDefault(cashier =>
            cashier.IsActive &&
            string.Equals(cashier.UserName, userName, StringComparison.OrdinalIgnoreCase) &&
            cashier.Password == password);

        return match is null ? null : ApiMapper.ToCashier(match);
    }

    public async Task UpdateLastLoginAsync(int cashierId)
    {
        var cashier = await api.GetAsync<ApiCashier>(api.Admin($"cashiers/{cashierId}"));
        if (cashier is null)
        {
            return;
        }

        cashier.LastLoginAt = DateTime.UtcNow;
        await api.PutAsync(api.Admin($"cashiers/{cashierId}"), cashier);
    }

    public async Task<Cashier?> GetByIdAsync(int cashierId)
    {
        var cashier = await api.GetAsync<ApiCashier>(api.Admin($"cashiers/{cashierId}"));
        return cashier is null ? null : ApiMapper.ToCashier(cashier);
    }
}
