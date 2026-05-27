using BlazorKompovNet.Models;

namespace BlazorKompovNet.Services;

public interface ICashierRepository
{
    Task<Cashier?> ValidateCredentialsAsync(string userName, string password);

    Task UpdateLastLoginAsync(int cashierId);

    Task<Cashier?> GetByIdAsync(int cashierId);
}
