using Payslip4All.Application.DTOs;
using Payslip4All.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Payslip4All.Infrastructure.Services;

public class PdfGenerationService : IPdfGenerationService
{
    // ── Colour palette ────────────────────────────────────────────────────
    private const string NavyHex    = "#1A3C5E";
    private const string GoldHex    = "#FFD700";
    private const string RowAltHex  = "#F5F5F5";
    private const string HeadingBgHex = "#E8EDF2";
    private const string DarkGreyHex = "#424242";
    private const string LightGreyHex = "#9E9E9E";

    public byte[] GeneratePayslip(PayslipDocument document)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // ── Section 1: Header ─────────────────────────────────
                    RenderHeader(col, document);

                    // ── Section 2: Employer Details ───────────────────────
                    RenderEmployerDetails(col, document);

                    // ── Section 3: Employee Details ───────────────────────
                    RenderEmployeeDetails(col, document);

                    // ── Section 4: Income Table ───────────────────────────
                    RenderIncomeTable(col, document);

                    // ── Section 5: Deductions Table ───────────────────────
                    RenderDeductionsTable(col, document);

                    // ── Section 6: Net Pay Summary ────────────────────────
                    RenderNetPaySummary(col, document);
                });
            });
        }).GeneratePdf();
    }

    // ── Section 1 ── Header ────────────────────────────────────────────────

    private static void RenderHeader(ColumnDescriptor col, PayslipDocument document)
    {
        var payslipRef = $"REF-{document.PayPeriod.Replace(" ", "").ToUpperInvariant()}";

        col.Item().Row(row =>
        {
            row.RelativeItem().Text(document.CompanyName)
                .Bold().FontSize(14).FontColor(NavyHex);

            row.RelativeItem().Column(c =>
            {
                c.Item().AlignRight().Text(document.PayPeriod)
                    .FontSize(9).FontColor(DarkGreyHex);
                c.Item().AlignRight().Text(payslipRef)
                    .FontSize(8).FontColor(LightGreyHex).Italic();
            });
        });

        col.Item().PaddingVertical(2).LineHorizontal(1).LineColor(NavyHex);
    }

    // ── Section 2 ── Employer Details ─────────────────────────────────────

    private static void RenderEmployerDetails(ColumnDescriptor col, PayslipDocument document)
    {
        col.Item().Column(c =>
        {
            c.Item().Background(HeadingBgHex).Padding(4)
                .Text("EMPLOYER DETAILS").Bold().FontSize(9).FontColor(NavyHex);

            c.Item().PaddingTop(4).Row(row =>
            {
                // Left: company name + address
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(document.CompanyName).SemiBold().FontSize(9);
                    if (!string.IsNullOrEmpty(document.CompanyAddress))
                        left.Item().Text(document.CompanyAddress).FontSize(8).FontColor(DarkGreyHex);
                });

                // Right: UIF + SARS
                row.RelativeItem().Column(right =>
                {
                    right.Item().Text(txt =>
                    {
                        txt.Span("UIF Reference: ").Bold().FontSize(8);
                        txt.Span(document.CompanyUifNumber ?? "—").FontSize(8);
                    });
                    right.Item().Text(txt =>
                    {
                        txt.Span("SARS PAYE No: ").Bold().FontSize(8);
                        txt.Span(document.CompanySarsPayeNumber ?? "—").FontSize(8);
                    });
                });
            });
        });
    }

    // ── Section 3 ── Employee Details ─────────────────────────────────────

    private static void RenderEmployeeDetails(ColumnDescriptor col, PayslipDocument document)
    {
        col.Item().Column(c =>
        {
            c.Item().Background(HeadingBgHex).Padding(4)
                .Text("EMPLOYEE DETAILS").Bold().FontSize(9).FontColor(NavyHex);

            c.Item().PaddingTop(4).Row(row =>
            {
                // Left: name, employee no, ID
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(document.EmployeeName).SemiBold().FontSize(9);
                    left.Item().Text(txt =>
                    {
                        txt.Span("Employee No: ").Bold().FontSize(8);
                        txt.Span(document.EmployeeNumber).FontSize(8);
                    });
                    left.Item().Text(txt =>
                    {
                        txt.Span("SA ID No: ").Bold().FontSize(8);
                        txt.Span(document.EmployeeIdNumber).FontSize(8);
                    });
                });

                // Right: occupation, start date, UIF ref
                row.RelativeItem().Column(right =>
                {
                    right.Item().Text(txt =>
                    {
                        txt.Span("Occupation: ").Bold().FontSize(8);
                        txt.Span(document.Occupation).FontSize(8);
                    });
                    right.Item().Text(txt =>
                    {
                        txt.Span("Start Date: ").Bold().FontSize(8);
                        txt.Span(document.EmployeeStartDate == default
                            ? "—"
                            : document.EmployeeStartDate.ToString("d MMM yyyy")).FontSize(8);
                    });
                    right.Item().Text(txt =>
                    {
                        txt.Span("UIF Reference: ").Bold().FontSize(8);
                        txt.Span(document.EmployeeUifReference ?? "—").FontSize(8);
                    });
                });
            });
        });
    }

    // ── Section 4 ── Income Table ──────────────────────────────────────────

    private static void RenderIncomeTable(ColumnDescriptor col, PayslipDocument document)
    {
        col.Item().Column(c =>
        {
            c.Item().Background(HeadingBgHex).Padding(4)
                .Text("INCOME").Bold().FontSize(9).FontColor(NavyHex);

            c.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(3);
                    cols.RelativeColumn(1);
                });

                // Header row
                table.Header(header =>
                {
                    header.Cell().Background(NavyHex).Padding(4)
                        .Text("Description").FontColor(Colors.White).Bold().FontSize(8);
                    header.Cell().Background(NavyHex).Padding(4).AlignRight()
                        .Text("Amount (R)").FontColor(Colors.White).Bold().FontSize(8);
                });

                // Basic Salary row (alternating background)
                table.Cell().Background(RowAltHex).Padding(4)
                    .Text("Basic Salary").FontSize(8);
                table.Cell().Background(RowAltHex).Padding(4).AlignRight()
                    .Text($"R {document.GrossEarnings:N2}").FontSize(8);

                // Divider row (empty visual separator)
                table.Cell().ColumnSpan(2).BorderBottom(0.5f).BorderColor(LightGreyHex).Height(1);

                // Gross Earnings total
                table.Cell().Background(RowAltHex).Padding(4)
                    .Text("Gross Earnings").Bold().FontSize(8);
                table.Cell().Background(RowAltHex).Padding(4).AlignRight()
                    .Text($"R {document.GrossEarnings:N2}").Bold().FontSize(8);
            });
        });
    }

    // ── Section 5 ── Deductions Table ─────────────────────────────────────

    private static void RenderDeductionsTable(ColumnDescriptor col, PayslipDocument document)
    {
        col.Item().Column(c =>
        {
            c.Item().Background(HeadingBgHex).Padding(4)
                .Text("DEDUCTIONS").Bold().FontSize(9).FontColor(NavyHex);

            c.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(3);
                    cols.RelativeColumn(1);
                });

                // Header row
                table.Header(header =>
                {
                    header.Cell().Background(NavyHex).Padding(4)
                        .Text("Description").FontColor(Colors.White).Bold().FontSize(8);
                    header.Cell().Background(NavyHex).Padding(4).AlignRight()
                        .Text("Amount (R)").FontColor(Colors.White).Bold().FontSize(8);
                });

                bool isAlt = false;

                // UIF Deduction
                string uifBg = isAlt ? RowAltHex : Colors.White;
                table.Cell().Background(uifBg).Padding(4)
                    .Text("UIF Deduction").FontSize(8);
                table.Cell().Background(uifBg).Padding(4).AlignRight()
                    .Text($"R {document.UifDeduction:N2}").FontSize(8);
                isAlt = !isAlt;

                // Loan deduction rows
                foreach (var (desc, amt) in document.LoanDeductions)
                {
                    string bg = isAlt ? RowAltHex : Colors.White;
                    table.Cell().Background(bg).Padding(4).Text(desc).FontSize(8);
                    table.Cell().Background(bg).Padding(4).AlignRight()
                        .Text($"R {amt:N2}").FontSize(8);
                    isAlt = !isAlt;
                }

                // Divider
                table.Cell().ColumnSpan(2).BorderBottom(0.5f).BorderColor(LightGreyHex).Height(1);

                // Total Deductions
                table.Cell().Background(RowAltHex).Padding(4)
                    .Text("Total Deductions").Bold().FontSize(8);
                table.Cell().Background(RowAltHex).Padding(4).AlignRight()
                    .Text($"R {document.TotalDeductions:N2}").Bold().FontSize(8);
            });
        });
    }

    // ── Section 6 ── Net Pay Summary ──────────────────────────────────────

    private static void RenderNetPaySummary(ColumnDescriptor col, PayslipDocument document)
    {
        // Net Pay band — navy background
        col.Item().Background(DarkGreyHex).Padding(12).Row(row =>
        {
            row.RelativeItem().Text("NET PAY")
                .Bold().FontSize(11).FontColor(Colors.White);

            row.RelativeItem().AlignRight()
                .Text($"R {document.NetPay:N2}")
                .Bold().FontSize(16).FontColor(GoldHex);
        });

        // Payment date
        if (document.PaymentDate != default)
        {
            col.Item().AlignRight()
                .Text($"Payment Date: {document.PaymentDate:d MMM yyyy}")
                .FontSize(9).FontColor(DarkGreyHex);
        }

        // Footer
        col.Item().PaddingTop(6).AlignCenter()
            .Text("This is a computer-generated payslip · Generated in accordance with South African Basic Conditions of Employment Act")
            .Italic().FontSize(7).FontColor(LightGreyHex);
    }
}
