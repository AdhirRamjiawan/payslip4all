namespace Payslip4All.Domain.Entities;

public class UnmatchedPaymentReturnRecord
{
    public Guid Id { get; private set; }
    public Guid PrimaryEvidenceId { get; set; }
    public string ProviderKey { get; set; } = string.Empty;
    public string CorrelationDisposition { get; set; } = string.Empty;
    public string GenericResultCode { get; set; } = "unmatched";
    public string DisplayMessage { get; set; } = "Your payment return could not be matched to a known top-up attempt.";
    public string? SafePayloadSnapshot { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public UnmatchedPaymentReturnRecord()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        ReceivedAt = DateTimeOffset.UtcNow;
    }
}
