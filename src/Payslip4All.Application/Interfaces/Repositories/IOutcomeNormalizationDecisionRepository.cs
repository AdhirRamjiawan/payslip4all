using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Interfaces.Repositories;

public interface IOutcomeNormalizationDecisionRepository
{
    Task AddAsync(OutcomeNormalizationDecision decision, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutcomeNormalizationDecision>> GetByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default);
}
