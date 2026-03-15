using Payslip4All.Application.DTOs.Employee;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
namespace Payslip4All.Application.Services;
public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repo;
    public EmployeeService(IEmployeeRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesForCompanyAsync(Guid companyId, Guid userId)
    {
        var employees = await _repo.GetAllByCompanyIdAsync(companyId, userId);
        return employees.Select(e => ToDto(e)).ToList();
    }
    public async Task<EmployeeDto?> GetEmployeeByIdAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id, userId);
        return e == null ? null : ToDto(e);
    }
    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.FirstName)) throw new ArgumentException("First name is required.", nameof(command.FirstName));
        if (command.MonthlyGrossSalary <= 0) throw new ArgumentException("Monthly gross salary must be greater than zero.", nameof(command.MonthlyGrossSalary));
        var employee = new Employee
        {
            FirstName = command.FirstName,
            LastName = command.LastName,
            IdNumber = command.IdNumber,
            EmployeeNumber = command.EmployeeNumber,
            StartDate = command.StartDate,
            Occupation = command.Occupation,
            UifReference = command.UifReference,
            MonthlyGrossSalary = command.MonthlyGrossSalary,
            CompanyId = command.CompanyId
        };
        await _repo.AddAsync(employee);
        return ToDto(employee);
    }
    public async Task<EmployeeDto?> UpdateEmployeeAsync(UpdateEmployeeCommand command)
    {
        var employee = await _repo.GetByIdAsync(command.Id, command.UserId);
        if (employee == null) return null;
        if (command.MonthlyGrossSalary <= 0) throw new ArgumentException("Monthly gross salary must be greater than zero.", nameof(command.MonthlyGrossSalary));
        employee.FirstName = command.FirstName;
        employee.LastName = command.LastName;
        employee.IdNumber = command.IdNumber;
        employee.EmployeeNumber = command.EmployeeNumber;
        employee.StartDate = command.StartDate;
        employee.Occupation = command.Occupation;
        employee.UifReference = command.UifReference;
        employee.MonthlyGrossSalary = command.MonthlyGrossSalary;
        await _repo.UpdateAsync(employee);
        return ToDto(employee);
    }
    public async Task<bool> DeleteEmployeeAsync(Guid id, Guid userId)
    {
        if (await _repo.HasPayslipsAsync(id)) return false;
        var employee = await _repo.GetByIdAsync(id, userId);
        if (employee == null) return false;
        await _repo.DeleteAsync(employee);
        return true;
    }
    private static EmployeeDto ToDto(Employee e) => new EmployeeDto
    {
        Id = e.Id,
        FirstName = e.FirstName,
        LastName = e.LastName,
        IdNumber = e.IdNumber,
        EmployeeNumber = e.EmployeeNumber,
        StartDate = e.StartDate,
        Occupation = e.Occupation,
        UifReference = e.UifReference,
        MonthlyGrossSalary = e.MonthlyGrossSalary,
        CompanyId = e.CompanyId,
        CreatedAt = e.CreatedAt
    };
}
