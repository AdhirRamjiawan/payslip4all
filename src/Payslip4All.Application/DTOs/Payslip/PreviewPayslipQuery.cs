namespace Payslip4All.Application.DTOs.Payslip;
public class PreviewPayslipQuery
{
    public Guid EmployeeId { get; set; }
    public int PayPeriodMonth { get; set; }
    public int PayPeriodYear { get; set; }
    public Guid UserId { get; set; }
}
