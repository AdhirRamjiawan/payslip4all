using Payslip4All.Domain.Enums;

namespace Payslip4All.Domain.Entities;

public class PaymentReturnEvidence
{
    public Guid Id { get; private set; }
    public string ProviderKey { get; set; } = string.Empty;
    public string SourceChannel { get; set; } = "BrowserReturn";
    public string? ProviderSessionReference { get; set; }
    public string? ProviderPaymentReference { get; set; }
    public string? MerchantPaymentReference { get; set; }
    public string? ReturnCorrelationToken { get; set; }
    public Guid? MatchedAttemptId { get; set; }
    public PaymentReturnCorrelationDisposition CorrelationDisposition { get; set; }
    public PaymentReturnClaimedOutcome? ClaimedOutcome { get; set; }
    public PaymentReturnTrustLevel TrustLevel { get; set; }
    public string? PaymentMethodCode { get; set; }
    public string? EnvironmentMode { get; set; }
    public bool SignatureVerified { get; set; }
    public bool SourceVerified { get; set; }
    public bool ServerConfirmed { get; set; }
    public decimal? ConfirmedChargedAmount { get; set; }
    public string? ConfirmedCurrencyCode { get; set; }
    public Guid? OwnerUserId { get; set; }
    public DateTimeOffset? EvidenceOccurredAt { get; set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset ValidatedAt { get; set; }
    public bool IsAtOrAfterAbandonmentThreshold { get; set; }
    public string? SafePayloadSnapshot { get; set; }
    public string? ValidationMessage { get; set; }

    public PaymentReturnEvidence()
    {
        Id = Guid.NewGuid();
        ReceivedAt = DateTimeOffset.UtcNow;
    }
}
