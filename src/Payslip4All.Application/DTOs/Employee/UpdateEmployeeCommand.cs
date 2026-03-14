namespace Payslip4All.Application.DTOs.Employee;
public class UpdateEmployeeCommand : CreateEmployeeCommand
{
    public Guid Id { get; set; }
}
