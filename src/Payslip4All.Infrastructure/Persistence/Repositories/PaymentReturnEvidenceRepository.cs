using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Infrastructure.Persistence.Repositories;

public sealed class PaymentReturnEvidenceRepository : IPaymentReturnEvidenceRepository
{
    private readonly PayslipDbContext _db;

    public PaymentReturnEvidenceRepository(PayslipDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(PaymentReturnEvidence evidence, CancellationToken cancellationToken = default)
    {
        await _db.PaymentReturnEvidences.AddAsync(evidence, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<PaymentReturnEvidence?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.PaymentReturnEvidences.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PaymentReturnEvidence>> GetByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default)
        => (await _db.PaymentReturnEvidences.AsNoTracking().Where(e => e.MatchedAttemptId == attemptId).ToListAsync(cancellationToken)).OrderBy(e => e.ReceivedAt).ToList();
}
