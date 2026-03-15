using Payslip4All.Application.Interfaces;
using Payslip4All.Infrastructure.Services;
using QuestPDF.Infrastructure;
using System.Diagnostics;

namespace Payslip4All.Infrastructure.Tests.Services;

/// <summary>
/// T065 — PDF generation performance benchmark (SC-002).
/// Asserts that <see cref="PdfGenerationService.GeneratePayslip"/> for a
/// representative payslip (1 loan deduction) completes in a median elapsed
/// time of less than 3 000 ms across ≥ 3 invocations.
///
/// QuestPDF's Community licence is used; no external tooling required.
/// The test uses a plain Stopwatch so it runs in any CI environment.
/// </summary>
public class PdfBenchmarkTests
{
    private const int Runs = 5;
    private const int MedianThresholdMs = 3_000;

    public PdfBenchmarkTests()
    {
        // QuestPDF Community licence must be declared before any document is
        // generated. Setting it here is idempotent across test runs.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static PayslipDocument BuildRepresentativeDocument() =>
        new PayslipDocument(
            CompanyName: "Acme Payroll Ltd",
            CompanyAddress: "123 Main Street, Johannesburg, 2000",
            EmployeeName: "Jane Doe",
            EmployeeNumber: "EMP-001",
            Occupation: "Software Engineer",
            PayPeriod: "January 2025",
            GrossEarnings: 35_000.00m,
            UifDeduction: 175.00m,
            LoanDeductions: new List<(string Description, decimal Amount)>
            {
                ("Vehicle Loan", 1_500.00m)
            },
            TotalDeductions: 1_675.00m,
            NetPay: 33_325.00m
        );

    [Fact]
    public void GeneratePayslip_MedianElapsedTime_IsUnder3000ms()
    {
        // Arrange
        var service = new PdfGenerationService();
        var document = BuildRepresentativeDocument();
        var elapsed = new long[Runs];

        // Warm-up: one call outside the timed loop to allow JIT compilation and
        // QuestPDF's internal initialisation to complete before we measure.
        _ = service.GeneratePayslip(document);

        // Act — timed runs
        for (int i = 0; i < Runs; i++)
        {
            var sw = Stopwatch.StartNew();
            var pdf = service.GeneratePayslip(document);
            sw.Stop();
            elapsed[i] = sw.ElapsedMilliseconds;

            // Basic sanity check: a generated PDF should not be empty.
            Assert.NotNull(pdf);
            Assert.NotEmpty(pdf);
        }

        // Assert — median < threshold
        Array.Sort(elapsed);
        long median = elapsed[Runs / 2];

        Assert.True(
            median < MedianThresholdMs,
            $"PDF generation median elapsed time was {median} ms, which exceeds the {MedianThresholdMs} ms SLO (SC-002). " +
            $"All run times: [{string.Join(", ", elapsed.Select(e => $"{e} ms"))}].");
    }
}
