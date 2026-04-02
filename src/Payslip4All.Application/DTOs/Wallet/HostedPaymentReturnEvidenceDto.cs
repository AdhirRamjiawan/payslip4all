using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.DTOs.Wallet;

public class HostedPaymentReturnEvidenceDto
{
    public Guid Id { get; set; }
    public string ProviderKey { get; set; } = string.Empty;
    public string SourceChannel { get; set; } = "BrowserReturn";
    public string? ProviderSessionReference { get; set; }
    public string? ProviderPaymentReference { get; set; }
    public string? ReturnCorrelationToken { get; set; }
    public Guid? MatchedAttemptId { get; set; }
    public PaymentReturnCorrelationDisposition CorrelationDisposition { get; set; }
    public PaymentReturnClaimedOutcome? ClaimedOutcome { get; set; }
    public PaymentReturnTrustLevel TrustLevel { get; set; }
    public decimal? ConfirmedChargedAmount { get; set; }
    public DateTimeOffset? EvidenceOccurredAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset ValidatedAt { get; set; }
    public bool IsAtOrAfterAbandonmentThreshold { get; set; }
    public string? SafePayloadSnapshot { get; set; }
    public string? ValidationMessage { get; set; }
}
