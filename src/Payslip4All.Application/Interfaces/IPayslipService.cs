using Payslip4All.Application.DTOs.Payslip;
namespace Payslip4All.Application.Interfaces;
public interface IPayslipService
{
    Task<PayslipResult> PreviewPayslipAsync(PreviewPayslipQuery query);
    Task<PayslipResult> GeneratePayslipAsync(GeneratePayslipCommand command);
    Task<IReadOnlyList<PayslipDto>> GetPayslipsForEmployeeAsync(Guid employeeId, Guid userId);
    Task<byte[]?> GetPdfAsync(Guid payslipId, Guid userId);
}
