using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence;

namespace Payslip4All.Infrastructure.Persistence.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly PayslipDbContext _db;

    public WalletRepository(PayslipDbContext db) => _db = db;

    public async Task<Wallet?> GetByUserIdAsync(Guid userId)
        => await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

    public async Task<Wallet?> GetByIdAsync(Guid id, Guid userId)
        => await _db.Wallets.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

    public async Task AddAsync(Wallet wallet)
    {
        await _db.Wallets.AddAsync(wallet);
        await _db.SaveChangesAsync();
        wallet.CapturePersistedState();
    }

    public async Task UpdateAsync(Wallet wallet)
    {
        try
        {
            _db.Wallets.Update(wallet);
            await _db.SaveChangesAsync();
            wallet.CapturePersistedState();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("Wallet was modified by another process.", ex);
        }
    }
}
