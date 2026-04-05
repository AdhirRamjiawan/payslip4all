using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.DTOs.Wallet;

public class HostedPaymentReturnResolutionDto
{
    public Guid EvidenceId { get; set; }
    public Guid? MatchedAttemptId { get; set; }
    public PaymentReturnCorrelationDisposition CorrelationDisposition { get; set; }
    public PaymentReturnClaimedOutcome? NormalizedOutcome { get; set; }
    public PaymentReturnTrustLevel TrustLevel { get; set; }
    public bool IsAuthoritative { get; set; }
    public bool SupersededAbandonment { get; set; }
    public bool SupersededNotConfirmed { get; set; }
    public bool ConflictWithAcceptedFinal { get; set; }
    public string TriggerSource { get; set; } = "BrowserReturn";
    public string? ReasonCode { get; set; }
    public string? ResolutionSummary { get; set; }
    public decimal? ConfirmedChargedAmount { get; set; }
    public string WalletEffect { get; set; } = "NoCredit";
    public Guid? WalletActivityId { get; set; }
}
