using Microsoft.EntityFrameworkCore;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence;

namespace Payslip4All.Infrastructure.Services;

public interface ICompanyService
{
    Task<List<Company>> GetCompaniesByUserAsync(int userId);
    Task<Company?> GetCompanyByIdAsync(int id);
    Task<Company> CreateCompanyAsync(int userId, Company company);
    Task<Company> UpdateCompanyAsync(Company company);
    Task DeleteCompanyAsync(int id);
}

public class CompanyService : ICompanyService
{
    private readonly PayslipDbContext _dbContext;

    public CompanyService(PayslipDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Company>> GetCompaniesByUserAsync(int userId)
    {
        return await _dbContext.Companies
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Company?> GetCompanyByIdAsync(int id)
    {
        return await _dbContext.Companies.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Company> CreateCompanyAsync(int userId, Company company)
    {
        company.UserId = userId;
        company.CreatedAt = DateTime.UtcNow;
        company.IsActive = true;

        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync();

        return company;
    }

    public async Task<Company> UpdateCompanyAsync(Company company)
    {
        company.UpdatedAt = DateTime.UtcNow;
        _dbContext.Companies.Update(company);
        await _dbContext.SaveChangesAsync();

        return company;
    }

    public async Task DeleteCompanyAsync(int id)
    {
        var company = await _dbContext.Companies.FirstOrDefaultAsync(c => c.Id == id);
        if (company != null)
        {
            company.IsActive = false;
            company.UpdatedAt = DateTime.UtcNow;
            _dbContext.Companies.Update(company);
            await _dbContext.SaveChangesAsync();
        }
    }
}
