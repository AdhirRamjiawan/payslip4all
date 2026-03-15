using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence;
namespace Payslip4All.Infrastructure.Persistence.Repositories;
public class CompanyRepository : ICompanyRepository
{
    private readonly PayslipDbContext _db;
    public CompanyRepository(PayslipDbContext db) => _db = db;
    public async Task<IReadOnlyList<Company>> GetAllByUserIdAsync(Guid userId)
        => await _db.Companies.Include(c => c.Employees).Where(c => c.UserId == userId).ToListAsync();
    public async Task<Company?> GetByIdAsync(Guid id, Guid userId)
        => await _db.Companies.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    public async Task<Company?> GetByIdWithEmployeesAsync(Guid id, Guid userId)
        => await _db.Companies.Include(c => c.Employees).FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    public async Task AddAsync(Company company)
    {
        await _db.Companies.AddAsync(company);
        await _db.SaveChangesAsync();
    }
    public async Task UpdateAsync(Company company)
    {
        _db.Companies.Update(company);
        await _db.SaveChangesAsync();
    }
    public async Task DeleteAsync(Company company)
    {
        _db.Companies.Remove(company);
        await _db.SaveChangesAsync();
    }
    public async Task<bool> HasEmployeesAsync(Guid id)
        => await _db.Employees.AnyAsync(e => e.CompanyId == id);
}
