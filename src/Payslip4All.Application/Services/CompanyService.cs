using Payslip4All.Application.DTOs.Company;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
namespace Payslip4All.Application.Services;
public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _repo;
    public CompanyService(ICompanyRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<CompanyDto>> GetCompaniesForUserAsync(Guid userId)
    {
        var companies = await _repo.GetAllByUserIdAsync(userId);
        return companies.Select(c => new CompanyDto
        {
            Id = c.Id, Name = c.Name, Address = c.Address, UserId = c.UserId,
            CreatedAt = c.CreatedAt, EmployeeCount = c.Employees.Count
        }).ToList();
    }
    public async Task<CompanyDto?> GetCompanyByIdAsync(Guid id, Guid userId)
    {
        var c = await _repo.GetByIdAsync(id, userId);
        if (c == null) return null;
        return new CompanyDto { Id = c.Id, Name = c.Name, Address = c.Address, UserId = c.UserId, CreatedAt = c.CreatedAt };
    }
    public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) throw new ArgumentException("Name is required.");
        var company = new Company { Name = command.Name, Address = command.Address, UserId = command.UserId };
        await _repo.AddAsync(company);
        return new CompanyDto { Id = company.Id, Name = company.Name, Address = company.Address, UserId = company.UserId, CreatedAt = company.CreatedAt };
    }
    public async Task<CompanyDto?> UpdateCompanyAsync(UpdateCompanyCommand command)
    {
        var company = await _repo.GetByIdAsync(command.Id, command.UserId);
        if (company == null) return null;
        company.Name = command.Name;
        company.Address = command.Address;
        await _repo.UpdateAsync(company);
        return new CompanyDto { Id = company.Id, Name = company.Name, Address = company.Address, UserId = company.UserId, CreatedAt = company.CreatedAt };
    }
    public async Task<bool> DeleteCompanyAsync(Guid id, Guid userId)
    {
        if (await _repo.HasEmployeesAsync(id)) return false;
        var company = await _repo.GetByIdAsync(id, userId);
        if (company == null) return false;
        await _repo.DeleteAsync(company);
        return true;
    }
}
