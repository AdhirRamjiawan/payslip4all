using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Infrastructure.Persistence.Repositories;

public class PayslipPricingRepository : IPayslipPricingRepository
{
    private readonly PayslipDbContext _db;

    public PayslipPricingRepository(PayslipDbContext db) => _db = db;

    public async Task<PayslipPricingSetting?> GetCurrentAsync()
        => (await _db.PayslipPricingSettings.ToListAsync())
            .OrderByDescending(p => p.UpdatedAt)
            .FirstOrDefault();

    public async Task AddAsync(PayslipPricingSetting setting)
    {
        await _db.PayslipPricingSettings.AddAsync(setting);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(PayslipPricingSetting setting)
    {
        _db.PayslipPricingSettings.Update(setting);
        await _db.SaveChangesAsync();
    }
}
