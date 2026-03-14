using Payslip4All.Application.DTOs.Employee;
namespace Payslip4All.Application.Interfaces;
public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> GetEmployeesForCompanyAsync(Guid companyId, Guid userId);
    Task<EmployeeDto?> GetEmployeeByIdAsync(Guid id, Guid userId);
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeCommand command);
    Task<EmployeeDto?> UpdateEmployeeAsync(UpdateEmployeeCommand command);
    Task<bool> DeleteEmployeeAsync(Guid id, Guid userId);
}
