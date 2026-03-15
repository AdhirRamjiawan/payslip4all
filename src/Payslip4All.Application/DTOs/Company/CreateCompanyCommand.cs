namespace Payslip4All.Application.DTOs.Company;
public class CreateCompanyCommand
{
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public Guid UserId { get; set; }
}
