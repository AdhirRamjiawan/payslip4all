using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence;
namespace Payslip4All.Infrastructure.Persistence.Repositories;
public class PayslipRepository : IPayslipRepository
{
    private readonly PayslipDbContext _db;
    public PayslipRepository(PayslipDbContext db) => _db = db;
    public async Task<IReadOnlyList<Payslip>> GetAllByEmployeeIdAsync(Guid employeeId, Guid userId)
        => await _db.Payslips
            .Include(p => p.LoanDeductions)
            .Include(p => p.Employee).ThenInclude(e => e.Company)
            .Where(p => p.EmployeeId == employeeId && p.Employee.Company.UserId == userId)
            .OrderByDescending(p => p.PayPeriodYear).ThenByDescending(p => p.PayPeriodMonth)
            .ToListAsync();
    public async Task<Payslip?> GetByIdAsync(Guid id, Guid userId)
        => await _db.Payslips
            .Include(p => p.LoanDeductions)
            .Include(p => p.Employee).ThenInclude(e => e.Company)
            .FirstOrDefaultAsync(p => p.Id == id && p.Employee.Company.UserId == userId);
    public async Task<bool> ExistsAsync(Guid employeeId, int month, int year)
        => await _db.Payslips.AnyAsync(p => p.EmployeeId == employeeId && p.PayPeriodMonth == month && p.PayPeriodYear == year);
    public async Task AddAsync(Payslip payslip)
    {
        await _db.Payslips.AddAsync(payslip);
        await _db.SaveChangesAsync();
    }
    public async Task DeleteAsync(Payslip payslip)
    {
        _db.Payslips.Remove(payslip);
        await _db.SaveChangesAsync();
    }
}
