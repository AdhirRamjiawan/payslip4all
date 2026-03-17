using Payslip4All.Application.DTOs;

namespace Payslip4All.Application.Interfaces;

public interface IPdfGenerationService
{
    byte[] GeneratePayslip(PayslipDocument document);
}
