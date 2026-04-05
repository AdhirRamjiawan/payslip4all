using Payslip4All.Domain.Entities;
using Xunit;

namespace Payslip4All.Infrastructure.Tests.Repositories;

public static class WalletTopUpAuditAssertions
{
    public static void AssertWalletCreditCorrelates(
        WalletTopUpAttempt attempt,
        PaymentReturnEvidence evidence,
        OutcomeNormalizationDecision decision)
    {
        Assert.Equal(evidence.Id, attempt.AuthoritativeEvidenceId);
        Assert.Equal(evidence.Id, decision.PaymentReturnEvidenceId);
        Assert.Equal(attempt.Id, decision.AttemptId);
    }
}
