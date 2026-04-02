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
}
