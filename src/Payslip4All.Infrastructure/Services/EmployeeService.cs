using Microsoft.EntityFrameworkCore;
using Payslip4All.Domain.Entities;
using Payslip4All.Infrastructure.Persistence;

namespace Payslip4All.Infrastructure.Services;

public interface IEmployeeService
{
    Task<List<Employee>> GetEmployeesByCompanyAsync(int companyId);
    Task<Employee?> GetEmployeeByIdAsync(int id);
    Task<Employee> CreateEmployeeAsync(Employee employee);
    Task<Employee> UpdateEmployeeAsync(Employee employee);
    Task DeleteEmployeeAsync(int id);
    Task<int> GetEmployeeCountByCompanyAsync(int companyId);
}

public class EmployeeService : IEmployeeService
{
    private readonly PayslipDbContext _dbContext;

    public EmployeeService(PayslipDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Employee>> GetEmployeesByCompanyAsync(int companyId)
    {
        return await _dbContext.Employees
            .Where(e => e.CompanyId == companyId && e.Status != EmployeeStatus.Terminated)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync();
    }

    public async Task<Employee?> GetEmployeeByIdAsync(int id)
    {
        return await _dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Employee> CreateEmployeeAsync(Employee employee)
    {
        employee.CreatedAt = DateTime.UtcNow;
        if (employee.Status == 0)
        {
            employee.Status = EmployeeStatus.Active;
        }

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync();

        return employee;
    }

    public async Task<Employee> UpdateEmployeeAsync(Employee employee)
    {
        employee.UpdatedAt = DateTime.UtcNow;
        _dbContext.Employees.Update(employee);
        await _dbContext.SaveChangesAsync();

        return employee;
    }

    public async Task DeleteEmployeeAsync(int id)
    {
        var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee != null)
        {
            employee.Status = EmployeeStatus.Terminated;
            employee.EmploymentEndDate = DateTime.UtcNow;
            employee.UpdatedAt = DateTime.UtcNow;
            _dbContext.Employees.Update(employee);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<int> GetEmployeeCountByCompanyAsync(int companyId)
    {
        return await _dbContext.Employees
            .CountAsync(e => e.CompanyId == companyId && e.Status != EmployeeStatus.Terminated);
    }
}
