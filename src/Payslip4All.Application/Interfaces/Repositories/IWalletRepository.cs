using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Interfaces.Repositories;

public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(Guid userId);
    Task<Wallet?> GetByIdAsync(Guid id, Guid userId);
    Task AddAsync(Wallet wallet);
    Task UpdateAsync(Wallet wallet);
}
