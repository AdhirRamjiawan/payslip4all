using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Interfaces.Repositories;

public interface IWalletActivityRepository
{
    Task<IReadOnlyList<WalletActivity>> GetByWalletIdAsync(Guid walletId);
    Task AddAsync(WalletActivity activity);
}
