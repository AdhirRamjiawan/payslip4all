using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Infrastructure.Persistence.Repositories;

public sealed class OutcomeNormalizationDecisionRepository : IOutcomeNormalizationDecisionRepository
{
    private readonly PayslipDbContext _db;

    public OutcomeNormalizationDecisionRepository(PayslipDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(OutcomeNormalizationDecision decision, CancellationToken cancellationToken = default)
    {
        await _db.OutcomeNormalizationDecisions.AddAsync(decision, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutcomeNormalizationDecision>> GetByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default)
        => (await _db.OutcomeNormalizationDecisions.AsNoTracking().Where(d => d.AttemptId == attemptId).ToListAsync(cancellationToken)).OrderBy(d => d.DecidedAt).ToList();
}
