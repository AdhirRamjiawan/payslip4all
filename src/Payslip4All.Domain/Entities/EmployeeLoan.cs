using Payslip4All.Domain.Enums;
namespace Payslip4All.Domain.Entities;
public class EmployeeLoan
{
    private int _persistedTermsCompleted;

    public Guid Id { get; private set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalLoanAmount { get; set; }
    public int NumberOfTerms { get; set; }
    public decimal MonthlyDeductionAmount { get; set; }
    public DateOnly PaymentStartDate { get; set; }
    public int TermsCompleted { get; private set; }
    public LoanStatus Status { get; private set; }
    public Guid EmployeeId { get; set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Employee Employee { get; set; } = null!;
    
    public EmployeeLoan()
    {
        Id = Guid.NewGuid();
        Status = LoanStatus.Active;
        TermsCompleted = 0;
        CreatedAt = DateTimeOffset.UtcNow;
        _persistedTermsCompleted = TermsCompleted;
    }
    
    public void IncrementTermsCompleted()
    {
        if (Status == LoanStatus.Completed)
            throw new InvalidOperationException("Cannot increment a completed loan.");
        TermsCompleted++;
        if (TermsCompleted == NumberOfTerms)
            Status = LoanStatus.Completed;
    }
    
    public bool IsActiveForPeriod(int month, int year)
    {
        var periodDate = new DateOnly(year, month, 1);
        return Status == LoanStatus.Active
            && periodDate >= PaymentStartDate
            && TermsCompleted < NumberOfTerms;
    }

    public int GetPersistedTermsCompleted() => _persistedTermsCompleted;

    public void CapturePersistedState() => _persistedTermsCompleted = TermsCompleted;

    public void RestorePersistedState()
        => RestoreTermsCompleted(_persistedTermsCompleted);

    public void RestoreTermsCompleted(int termsCompleted)
    {
        TermsCompleted = termsCompleted;
        Status = TermsCompleted >= NumberOfTerms
            ? LoanStatus.Completed
            : LoanStatus.Active;
    }
}
