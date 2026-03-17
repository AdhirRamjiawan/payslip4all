using Payslip4All.Application.DTOs;
using Payslip4All.Infrastructure.Services;
using QuestPDF.Infrastructure;
using System.Diagnostics;

namespace Payslip4All.Infrastructure.Tests.Services;

/// <summary>
/// T008 — TDD tests for <see cref="PdfGenerationService"/> 6-section layout.
/// Written FIRST (before implementation) per constitution Principle I (TDD).
/// Tests must be RED before T010 implementation begins.
/// </summary>
public class PdfGenerationServiceTests
{
    private readonly PdfGenerationService _service;
    private readonly PayslipDocument _fullyPopulatedDocument;

    public PdfGenerationServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _service = new PdfGenerationService();

        _fullyPopulatedDocument = new PayslipDocument(
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
            NetPay: 33_325.00m,
            CompanyUifNumber: "U123456",
            CompanySarsPayeNumber: "7654321A",
            EmployeeIdNumber: "9001015009087",
            EmployeeStartDate: new DateOnly(2021, 3, 1),
            EmployeeUifReference: "UIF-EMP-001",
            PaymentDate: new DateOnly(2025, 1, 31)
        );
    }

    // ──────────────────────────────────────────────────────────────────────
    // (a) Smoke test — returns non-empty byte[]
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void GeneratePayslip_FullyPopulatedDocument_ReturnsNonEmptyByteArray()
    {
        var result = _service.GeneratePayslip(_fullyPopulatedDocument);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    // ──────────────────────────────────────────────────────────────────────
    // (b) Section-presence tests — byte array non-null and exceeds minimum size
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void GeneratePayslip_FullyPopulatedDocument_PdfExceedsMinimumByteThreshold()
    {
        // A PDF with 6 sections of real content should be at least 5 KB
        const int MinimumBytes = 5_000;

        var result = _service.GeneratePayslip(_fullyPopulatedDocument);

        Assert.NotNull(result);
        Assert.True(result.Length > MinimumBytes,
            $"Expected PDF size > {MinimumBytes} bytes but got {result.Length} bytes.");
    }

    [Fact]
    public void GeneratePayslip_FullyPopulatedDocument_StartsWithPdfMagicBytes()
    {
        var result = _service.GeneratePayslip(_fullyPopulatedDocument);

        // PDF files begin with %PDF (0x25 0x50 0x44 0x46)
        Assert.True(result.Length >= 4, "PDF must be at least 4 bytes.");
        Assert.Equal(0x25, result[0]); // %
        Assert.Equal(0x50, result[1]); // P
        Assert.Equal(0x44, result[2]); // D
        Assert.Equal(0x46, result[3]); // F
    }

    // ──────────────────────────────────────────────────────────────────────
    // (c) No-loan-deductions — generation succeeds with empty LoanDeductions
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void GeneratePayslip_EmptyLoanDeductions_ReturnsNonEmptyByteArray()
    {
        var docNoLoans = new PayslipDocument(
            CompanyName: "Simple Corp",
            CompanyAddress: null,
            EmployeeName: "John Smith",
            EmployeeNumber: "EMP-002",
            Occupation: "Accountant",
            PayPeriod: "February 2025",
            GrossEarnings: 20_000.00m,
            UifDeduction: 100.00m,
            LoanDeductions: new List<(string Description, decimal Amount)>(),
            TotalDeductions: 100.00m,
            NetPay: 19_900.00m,
            CompanyUifNumber: null,
            CompanySarsPayeNumber: null,
            EmployeeIdNumber: "8501015009083",
            EmployeeStartDate: new DateOnly(2020, 6, 1),
            EmployeeUifReference: null,
            PaymentDate: new DateOnly(2025, 2, 28)
        );

        var result = _service.GeneratePayslip(docNoLoans);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    // ──────────────────────────────────────────────────────────────────────
    // (d) Performance guard — generation under 500 ms (single run)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void GeneratePayslip_SingleRun_CompletesUnder500Ms()
    {
        const int MaxMs = 500;

        // Warm-up to avoid JIT overhead in the timed measurement
        _ = _service.GeneratePayslip(_fullyPopulatedDocument);

        var sw = Stopwatch.StartNew();
        _ = _service.GeneratePayslip(_fullyPopulatedDocument);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < MaxMs,
            $"PDF generation took {sw.ElapsedMilliseconds} ms, expected < {MaxMs} ms.");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Additional: null-safe optional fields (UIF/SARS/UifReference = null)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void GeneratePayslip_NullOptionalFields_DoesNotThrow()
    {
        var docNullOptionals = new PayslipDocument(
            CompanyName: "Minimal Corp",
            CompanyAddress: null,
            EmployeeName: "Alice Wonder",
            EmployeeNumber: "EMP-003",
            Occupation: "Manager",
            PayPeriod: "March 2025",
            GrossEarnings: 50_000.00m,
            UifDeduction: 148.72m,
            LoanDeductions: new List<(string Description, decimal Amount)>(),
            TotalDeductions: 148.72m,
            NetPay: 49_851.28m,
            CompanyUifNumber: null,
            CompanySarsPayeNumber: null,
            EmployeeIdNumber: "7711225800082",
            EmployeeStartDate: new DateOnly(2019, 1, 15),
            EmployeeUifReference: null,
            PaymentDate: new DateOnly(2025, 3, 31)
        );

        var exception = Record.Exception(() => _service.GeneratePayslip(docNullOptionals));
        Assert.Null(exception);
    }
}
