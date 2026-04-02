using Payslip4All.Domain.Entities;

namespace Payslip4All.Application.Interfaces.Repositories;

public interface IPaymentReturnEvidenceRepository
{
    Task AddAsync(PaymentReturnEvidence evidence, CancellationToken cancellationToken = default);
    Task<PaymentReturnEvidence?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentReturnEvidence>> GetByAttemptIdAsync(Guid attemptId, CancellationToken cancellationToken = default);
}
