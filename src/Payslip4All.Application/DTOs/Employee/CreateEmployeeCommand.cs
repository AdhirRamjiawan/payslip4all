namespace Payslip4All.Application.DTOs.Employee;
public class CreateEmployeeCommand
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string IdNumber { get; set; } = "";
    public string EmployeeNumber { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public string Occupation { get; set; } = "";
    public string? UifReference { get; set; }
    public decimal MonthlyGrossSalary { get; set; }
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
}
