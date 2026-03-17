using Payslip4All.Domain.Entities;

namespace Payslip4All.Domain.Tests.Entities;

public class CompanyTests
{
    // ──────────────────────────────────────────────────────────────────────
    // UifNumber property
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void UifNumber_DefaultsToNull()
    {
        var company = new Company();
        Assert.Null(company.UifNumber);
    }

    [Fact]
    public void UifNumber_AcceptsNullAssignment()
    {
        var company = new Company { UifNumber = null };
        Assert.Null(company.UifNumber);
    }

    [Fact]
    public void UifNumber_AcceptsValueUpTo50Chars()
    {
        var value = new string('U', 50);
        var company = new Company { UifNumber = value };
        Assert.Equal(value, company.UifNumber);
    }

    [Fact]
    public void UifNumber_ApplicationGuard_RejectsValueOver50Chars()
    {
        // The Application layer is responsible for enforcing max-length.
        // The guard should throw ArgumentException for values > 50 chars.
        var tooLong = new string('U', 51);
        Assert.Throws<ArgumentException>(() => CompanyGuard.ValidateUifNumber(tooLong));
    }

    [Fact]
    public void UifNumber_AcceptsTypicalSaUifReference()
    {
        var company = new Company { UifNumber = "U123456789" };
        Assert.Equal("U123456789", company.UifNumber);
    }

    // ──────────────────────────────────────────────────────────────────────
    // SarsPayeNumber property
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void SarsPayeNumber_DefaultsToNull()
    {
        var company = new Company();
        Assert.Null(company.SarsPayeNumber);
    }

    [Fact]
    public void SarsPayeNumber_AcceptsNullAssignment()
    {
        var company = new Company { SarsPayeNumber = null };
        Assert.Null(company.SarsPayeNumber);
    }

    [Fact]
    public void SarsPayeNumber_AcceptsValueUpTo30Chars()
    {
        var value = new string('7', 30);
        var company = new Company { SarsPayeNumber = value };
        Assert.Equal(value, company.SarsPayeNumber);
    }

    [Fact]
    public void SarsPayeNumber_ApplicationGuard_RejectsValueOver30Chars()
    {
        var tooLong = new string('7', 31);
        Assert.Throws<ArgumentException>(() => CompanyGuard.ValidateSarsPayeNumber(tooLong));
    }

    [Fact]
    public void SarsPayeNumber_AcceptsTypicalSarsPayeValue()
    {
        var company = new Company { SarsPayeNumber = "7654321A" };
        Assert.Equal("7654321A", company.SarsPayeNumber);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Both fields — round-trip compatibility
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Company_WithBothUifAndSarsFields_RetainsAllExistingProperties()
    {
        var userId = Guid.NewGuid();
        var company = new Company
        {
            Name = "Test Corp",
            Address = "1 Test Road",
            UserId = userId,
            UifNumber = "U987654",
            SarsPayeNumber = "1234567B"
        };

        Assert.Equal("Test Corp", company.Name);
        Assert.Equal("1 Test Road", company.Address);
        Assert.Equal(userId, company.UserId);
        Assert.Equal("U987654", company.UifNumber);
        Assert.Equal("1234567B", company.SarsPayeNumber);
    }
}
