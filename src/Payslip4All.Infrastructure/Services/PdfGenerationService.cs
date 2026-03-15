using Payslip4All.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace Payslip4All.Infrastructure.Services;
public class PdfGenerationService : IPdfGenerationService
{
    public byte[] GeneratePayslip(PayslipDocument document)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.Content().Column(col =>
                {
                    col.Item().Text(document.CompanyName).Bold().FontSize(16);
                    if (!string.IsNullOrEmpty(document.CompanyAddress))
                        col.Item().Text(document.CompanyAddress);
                    col.Item().PaddingVertical(10).LineHorizontal(1);
                    col.Item().Text($"Employee: {document.EmployeeName}");
                    col.Item().Text($"Employee #: {document.EmployeeNumber}");
                    col.Item().Text($"Occupation: {document.Occupation}");
                    col.Item().Text($"Pay Period: {document.PayPeriod}");
                    col.Item().PaddingVertical(5).LineHorizontal(1);
                    col.Item().Text($"Gross Earnings: R{document.GrossEarnings:N2}");
                    col.Item().Text($"UIF Deduction: R{document.UifDeduction:N2}");
                    foreach (var (desc, amt) in document.LoanDeductions)
                        col.Item().Text($"{desc}: R{amt:N2}");
                    col.Item().PaddingVertical(5).LineHorizontal(1);
                    col.Item().Text($"Total Deductions: R{document.TotalDeductions:N2}").Bold();
                    col.Item().Text($"Net Pay: R{document.NetPay:N2}").Bold().FontSize(14);
                });
            });
        }).GeneratePdf();
    }
}
