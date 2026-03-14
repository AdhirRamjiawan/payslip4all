using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence;
namespace Payslip4All.Infrastructure.Persistence.Repositories;
public class EmployeeRepository : IEmployeeRepository
{
    private readonly PayslipDbContext _db;
    public EmployeeRepository(PayslipDbContext db) => _db = db;
    public async Task<IReadOnlyList<Employee>> GetAllByCompanyIdAsync(Guid companyId, Guid userId)
        => await _db.Employees
            .Include(e => e.Company)
            .Where(e => e.CompanyId == companyId && e.Company.UserId == userId)
            .ToListAsync();
    public async Task<Employee?> GetByIdAsync(Guid id, Guid userId)
        => await _db.Employees
            .Include(e => e.Company)
            .FirstOrDefaultAsync(e => e.Id == id && e.Company.UserId == userId);
    public async Task<Employee?> GetByIdWithLoansAsync(Guid id, Guid userId)
        => await _db.Employees
            .Include(e => e.Company)
            .Include(e => e.Loans)
            .FirstOrDefaultAsync(e => e.Id == id && e.Company.UserId == userId);
    public async Task AddAsync(Employee employee)
    {
        await _db.Employees.AddAsync(employee);
        await _db.SaveChangesAsync();
    }
    public async Task UpdateAsync(Employee employee)
    {
        _db.Employees.Update(employee);
        await _db.SaveChangesAsync();
    }
    public async Task DeleteAsync(Employee employee)
    {
        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();
    }
    public async Task<bool> HasPayslipsAsync(Guid id)
        => await _db.Payslips.AnyAsync(p => p.EmployeeId == id);
}
