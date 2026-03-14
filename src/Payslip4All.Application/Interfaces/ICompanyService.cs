using Payslip4All.Application.DTOs.Company;
namespace Payslip4All.Application.Interfaces;
public interface ICompanyService
{
    Task<IReadOnlyList<CompanyDto>> GetCompaniesForUserAsync(Guid userId);
    Task<CompanyDto?> GetCompanyByIdAsync(Guid id, Guid userId);
    Task<CompanyDto> CreateCompanyAsync(CreateCompanyCommand command);
    Task<CompanyDto?> UpdateCompanyAsync(UpdateCompanyCommand command);
    Task<bool> DeleteCompanyAsync(Guid id, Guid userId);
}
