namespace Payslip4All.Application.DTOs.Company;
public class CompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int EmployeeCount { get; set; }
}
