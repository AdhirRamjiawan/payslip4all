using Payslip4All.Application.Interfaces;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB;

/// <summary>
/// No-op implementation of <see cref="IUnitOfWork"/> for DynamoDB.
/// DynamoDB repositories commit on each SDK call; there is nothing to flush.
/// </summary>
public sealed class DynamoDbUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public Task BeginTransactionAsync()
        => Task.CompletedTask;

    public Task CommitTransactionAsync()
        => Task.CompletedTask;

    public Task RollbackTransactionAsync()
        => Task.CompletedTask;
}
