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
            ConfirmedChargedAmount = evidence.ConfirmedChargedAmount
        };

        if (matchedAttempt == null || evidence.CorrelationDisposition != PaymentReturnCorrelationDisposition.ExactMatch)
        {
            resolution.ReasonCode = "unmatched";
            resolution.ResolutionSummary = "Return evidence could not be matched to a single hosted top-up attempt.";
            return Task.FromResult(resolution);
        }

        if (matchedAttempt.Status is WalletTopUpAttemptStatus.Completed or WalletTopUpAttemptStatus.Cancelled or WalletTopUpAttemptStatus.Expired)
        {
            resolution.ConflictWithAcceptedFinal = true;
            resolution.ReasonCode = "final_outcome_already_accepted";
            resolution.ResolutionSummary = "An authoritative final outcome was already accepted for this attempt.";
            return Task.FromResult(resolution);
        }

        if (evidence.IsAtOrAfterAbandonmentThreshold && matchedAttempt.Status == WalletTopUpAttemptStatus.Abandoned)
        {
            if (evidence.TrustLevel == PaymentReturnTrustLevel.Trustworthy &&
                evidence.ClaimedOutcome is PaymentReturnClaimedOutcome.Completed or PaymentReturnClaimedOutcome.Cancelled or PaymentReturnClaimedOutcome.Expired)
            {
                resolution.IsAuthoritative = true;
                resolution.SupersededAbandonment = true;
                resolution.WalletEffect = evidence.ClaimedOutcome == PaymentReturnClaimedOutcome.Completed ? "CreditWallet" : "NoCredit";
                resolution.ReasonCode = "trustworthy_late_final";
                resolution.ResolutionSummary = "Trustworthy late evidence superseded abandonment.";
                return Task.FromResult(resolution);
            }

            resolution.ReasonCode = "abandoned_without_trustworthy_final";
            resolution.ResolutionSummary = "Abandoned attempt remains unchanged because late evidence was not trustworthy enough.";
            return Task.FromResult(resolution);
        }

        if (evidence.TrustLevel == PaymentReturnTrustLevel.Untrusted)
        {
            resolution.NormalizedOutcome = PaymentReturnClaimedOutcome.Unknown;
            resolution.ReasonCode = "untrusted_return";
            resolution.ResolutionSummary = "Return evidence was untrusted and cannot settle the wallet.";
            return Task.FromResult(resolution);
        }

        if (evidence.TrustLevel == PaymentReturnTrustLevel.LowConfidence)
        {
            resolution.NormalizedOutcome = PaymentReturnClaimedOutcome.Unknown;
            resolution.ReasonCode = "low_confidence_return";
            resolution.ResolutionSummary = "Return evidence requires manual verification before wallet settlement.";
            return Task.FromResult(resolution);
        }

        if (evidence.ClaimedOutcome == PaymentReturnClaimedOutcome.Completed)
        {
            resolution.IsAuthoritative = true;
            resolution.WalletEffect = "CreditWallet";
            resolution.ReasonCode = "trustworthy_completed";
            resolution.ResolutionSummary = "Trustworthy completion evidence accepted.";
            return Task.FromResult(resolution);
        }

        if (evidence.ClaimedOutcome == PaymentReturnClaimedOutcome.Cancelled)
        {
            resolution.IsAuthoritative = true;
            resolution.WalletEffect = "NoCredit";
            resolution.ReasonCode = "trustworthy_cancelled";
            resolution.ResolutionSummary = "Trustworthy cancellation evidence accepted.";
            return Task.FromResult(resolution);
        }

        if (evidence.ClaimedOutcome == PaymentReturnClaimedOutcome.Expired)
        {
            resolution.IsAuthoritative = true;
            resolution.WalletEffect = "NoCredit";
            resolution.ReasonCode = "trustworthy_expired";
            resolution.ResolutionSummary = "Trustworthy expiry evidence accepted.";
            return Task.FromResult(resolution);
        }

        resolution.NormalizedOutcome = PaymentReturnClaimedOutcome.Unknown;
        resolution.ReasonCode = "unknown_outcome";
        resolution.ResolutionSummary = "Return evidence did not contain a usable final outcome.";
        return Task.FromResult(resolution);
    }
}
