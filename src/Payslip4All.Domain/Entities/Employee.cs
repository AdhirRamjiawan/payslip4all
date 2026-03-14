namespace Payslip4All.Domain.Entities;
public class Employee
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public string Occupation { get; set; } = string.Empty;
    public string? UifReference { get; set; }
    public decimal MonthlyGrossSalary { get; set; }
    public Guid CompanyId { get; set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Company Company { get; set; } = null!;
    public List<EmployeeLoan> Loans { get; set; } = new();
    public List<Payslip> Payslips { get; set; } = new();
    
    public Employee()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
