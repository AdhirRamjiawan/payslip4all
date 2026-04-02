using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Interfaces.Repositories;

public interface IUnmatchedPaymentReturnRecordRepository
{
    Task AddAsync(UnmatchedPaymentReturnRecord record, CancellationToken cancellationToken = default);
    Task<UnmatchedPaymentReturnRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
