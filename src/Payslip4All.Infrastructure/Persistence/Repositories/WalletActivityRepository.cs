using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Infrastructure.Persistence.Repositories;

public class WalletActivityRepository : IWalletActivityRepository
{
    private readonly PayslipDbContext _db;

    public WalletActivityRepository(PayslipDbContext db) => _db = db;

    public async Task<IReadOnlyList<WalletActivity>> GetByWalletIdAsync(Guid walletId)
    {
        var query = _db.WalletActivities.Where(a => a.WalletId == walletId);

        if (_db.Database.IsSqlite())
        {
            return (await query.ToListAsync())
                .OrderByDescending(a => a.OccurredAt)
                .ToList();
        }

        return await query
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync();
    }

    public async Task AddAsync(WalletActivity activity)
    {
        await _db.WalletActivities.AddAsync(activity);
        await _db.SaveChangesAsync();
    }
}
