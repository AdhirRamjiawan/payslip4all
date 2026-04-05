using Microsoft.EntityFrameworkCore;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;

namespace Payslip4All.Infrastructure.Persistence.Repositories;

public sealed class UnmatchedPaymentReturnRecordRepository : IUnmatchedPaymentReturnRecordRepository
{
    private readonly PayslipDbContext _db;

    public UnmatchedPaymentReturnRecordRepository(PayslipDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(UnmatchedPaymentReturnRecord record, CancellationToken cancellationToken = default)
    {
        await _db.UnmatchedPaymentReturnRecords.AddAsync(record, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<UnmatchedPaymentReturnRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.UnmatchedPaymentReturnRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UnmatchedPaymentReturnRecord>> GetForAdminReviewAsync(Guid? id, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CancellationToken cancellationToken = default)
    {
        var query = _db.UnmatchedPaymentReturnRecords.AsNoTracking().AsQueryable();

        if (id.HasValue)
            query = query.Where(r => r.Id == id.Value);
        if (fromUtc.HasValue)
            query = query.Where(r => r.ReceivedAt >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(r => r.ReceivedAt <= toUtc.Value);

        return await query
            .OrderByDescending(r => r.ReceivedAt)
            .ToListAsync(cancellationToken);
    }
}
