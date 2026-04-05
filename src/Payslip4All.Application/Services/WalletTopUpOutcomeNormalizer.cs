using Payslip4All.Application.DTOs.Wallet;
using Payslip4All.Application.Interfaces;
using Payslip4All.Domain.Entities;
using Payslip4All.Domain.Enums;

namespace Payslip4All.Application.Services;

public sealed class WalletTopUpOutcomeNormalizer : IWalletTopUpOutcomeNormalizer
{
    public Task<HostedPaymentReturnResolutionDto> NormalizeAsync(
        PaymentReturnEvidence evidence,
        WalletTopUpAttempt? matchedAttempt,
        CancellationToken cancellationToken = default)
    {
        var resolution = new HostedPaymentReturnResolutionDto
        {
            EvidenceId = evidence.Id,
            MatchedAttemptId = matchedAttempt?.Id,
            CorrelationDisposition = evidence.CorrelationDisposition,
            NormalizedOutcome = evidence.ClaimedOutcome,
            TrustLevel = evidence.TrustLevel,
            ConfirmedChargedAmount = evidence.ConfirmedChargedAmount,
            TriggerSource = evidence.SourceChannel
        };

        if (matchedAttempt != null && matchedAttempt.IsFinalForSettlement)
        {
            resolution.ConflictWithAcceptedFinal = true;
            resolution.ReasonCode = "duplicate_finalized";
            resolution.ResolutionSummary = "Top-up not confirmed";
            resolution.NormalizedOutcome = PaymentReturnClaimedOutcome.Unknown;
            return Task.FromResult(resolution);
        }

        if (matchedAttempt == null || evidence.CorrelationDisposition != PaymentReturnCorrelationDisposition.ExactMatch)
        {
            resolution.ReasonCode = "not_confirmed";
            resolution.ResolutionSummary = "Top-up not confirmed";
            resolution.NormalizedOutcome = PaymentReturnClaimedOutcome.Unknown;
            return Task.FromResult(resolution);
        }

        if (!string.Equals(evidence.SourceChannel, "PayFastNotify", StringComparison.Ordinal))
        {
            resolution.ReasonCode = "browser_return_informational";
            resolution.ResolutionSummary = "Top-up not confirmed";
            resolution.NormalizedOutcome = PaymentReturnClaimedOutcome.Unknown;
            return Task.FromResult(resolution);
        }

        if (evidence.TrustLevel != PaymentReturnTrustLevel.Trustworthy
            || !evidence.SignatureVerified
            || !evidence.SourceVerified
            || !evidence.ServerConfirmed)
        {
            resolution.ReasonCode = "not_confirmed";
            resolution.ResolutionSummary = "Top-up not confirmed";
            resolution.NormalizedOutcome = PaymentReturnClaimedOutcome.Unknown;
            return Task.FromResult(resolution);
        }

        if (!string.Equals(evidence.PaymentMethodCode, "cc", StringComparison.OrdinalIgnoreCase))
        {
            resolution.ReasonCode = "non_card_payment";
            resolution.ResolutionSummary = "Top-up not confirmed";
            resolution.NormalizedOutcome = PaymentReturnClaimedOutcome.Unknown;
            return Task.FromResult(resolution);
        }

        if (!string.Equals(evidence.ConfirmedCurrencyCode, matchedAttempt.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            resolution.ReasonCode = "currency_mismatch";
            resolution.ResolutionSummary = "Top-up not confirmed";
            resolution.NormalizedOutcome = PaymentReturnClaimedOutcome.Unknown;
            return Task.FromResult(resolution);
        }

        if (evidence.OwnerUserId.HasValue && evidence.OwnerUserId.Value != matchedAttempt.UserId)
        {
            resolution.ReasonCode = "foreign_owner";
            resolution.ResolutionSummary = "Top-up not confirmed";
            resolution.NormalizedOutcome = PaymentReturnClaimedOutcome.Unknown;
            return Task.FromResult(resolution);
        }

        switch (evidence.ClaimedOutcome)
        {
            case PaymentReturnClaimedOutcome.Completed when evidence.ConfirmedChargedAmount.HasValue:
                resolution.IsAuthoritative = true;
                resolution.WalletEffect = "CreditCreated";
                resolution.ReasonCode = "trustworthy_completed";
                resolution.ResolutionSummary = "Trustworthy PayFast confirmation accepted.";
                return Task.FromResult(resolution);

            case PaymentReturnClaimedOutcome.Cancelled:
                resolution.IsAuthoritative = true;
                resolution.WalletEffect = "NoCredit";
                resolution.ReasonCode = "trustworthy_cancelled";
                resolution.ResolutionSummary = "Trustworthy PayFast cancellation accepted.";
                return Task.FromResult(resolution);

            default:
                resolution.ReasonCode = "not_confirmed";
                resolution.ResolutionSummary = "Top-up not confirmed";
                resolution.NormalizedOutcome = PaymentReturnClaimedOutcome.Unknown;
                return Task.FromResult(resolution);
        }
    }
}
