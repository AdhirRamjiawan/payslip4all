namespace Payslip4All.Application.DTOs.Wallet;

public class HostedPaymentReturnResult
{
    public Guid AttemptId { get; set; }
    public HostedPaymentOutcome Outcome { get; set; }
    public string? ProviderSessionReference { get; set; }
    public string? ProviderPaymentReference { get; set; }
    public decimal? ConfirmedChargedAmount { get; set; }
    public DateTimeOffset ValidatedAt { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public string DisplayMessage { get; set; } = string.Empty;
}

public enum HostedPaymentOutcome
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2,
    Cancelled = 3,
    Expired = 4,
    Unmatched = 5,
    Unverified = 6
}
