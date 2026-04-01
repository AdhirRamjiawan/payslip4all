using Payslip4All.Application.DTOs.Wallet;

namespace Payslip4All.Application.Interfaces;

public interface IWalletService
{
    Task<WalletDto> GetWalletAsync(Guid userId);
    Task<WalletDto> TopUpAsync(AddWalletCreditCommand command);
    Task<IReadOnlyList<WalletActivityDto>> GetActivitiesAsync(Guid userId);
    Task<bool> TryDebitAsync(Guid userId, decimal amount, string description, string? referenceType = null, string? referenceId = null);
}
