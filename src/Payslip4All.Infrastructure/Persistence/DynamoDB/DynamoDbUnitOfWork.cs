using Payslip4All.Application.Interfaces;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB;

/// <summary>
/// Save-only implementation of <see cref="IUnitOfWork"/> for DynamoDB.
/// DynamoDB repositories commit on each SDK call and this adapter does not expose
/// transactional guarantees through the relational unit-of-work contract.
/// </summary>
public sealed class DynamoDbUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public Task BeginTransactionAsync()
        => throw new NotSupportedException(
            "DynamoDB provider does not support IUnitOfWork transactions. Use idempotent writes or DynamoDB transactional APIs explicitly.");

    public Task CommitTransactionAsync()
        => throw new NotSupportedException(
            "DynamoDB provider does not support IUnitOfWork transactions. Use idempotent writes or DynamoDB transactional APIs explicitly.");

    public Task RollbackTransactionAsync()
        => throw new NotSupportedException(
            "DynamoDB provider does not support IUnitOfWork transactions. Use idempotent writes or DynamoDB transactional APIs explicitly.");
}
