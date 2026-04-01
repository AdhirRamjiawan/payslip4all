using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence;
namespace Payslip4All.Infrastructure.Persistence.Repositories;
public class LoanRepository : ILoanRepository
{
    private readonly PayslipDbContext _db;
    public LoanRepository(PayslipDbContext db) => _db = db;
    public async Task<IReadOnlyList<EmployeeLoan>> GetAllByEmployeeIdAsync(Guid employeeId, Guid userId)
        => await _db.EmployeeLoans
            .Include(l => l.Employee).ThenInclude(e => e.Company)
            .Where(l => l.EmployeeId == employeeId && l.Employee.Company.UserId == userId)
            .ToListAsync();
    public async Task<EmployeeLoan?> GetByIdAsync(Guid id, Guid userId)
        => await _db.EmployeeLoans
            .Include(l => l.Employee).ThenInclude(e => e.Company)
            .FirstOrDefaultAsync(l => l.Id == id && l.Employee.Company.UserId == userId);
    public async Task AddAsync(EmployeeLoan loan)
    {
        await _db.EmployeeLoans.AddAsync(loan);
        await _db.SaveChangesAsync();
        loan.CapturePersistedState();
    }
    public async Task UpdateAsync(EmployeeLoan loan)
    {
        _db.EmployeeLoans.Update(loan);
        await _db.SaveChangesAsync();
        loan.CapturePersistedState();
    }
    public async Task DeleteAsync(EmployeeLoan loan)
    {
        _db.EmployeeLoans.Remove(loan);
        await _db.SaveChangesAsync();
    }
}
