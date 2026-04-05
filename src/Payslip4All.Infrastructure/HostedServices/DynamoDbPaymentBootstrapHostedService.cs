using Microsoft.Extensions.Hosting;
using Payslip4All.Infrastructure.Persistence.DynamoDB;

namespace Payslip4All.Infrastructure.HostedServices;

public sealed class DynamoDbPaymentBootstrapHostedService : IHostedService
{
    private readonly DynamoDbTableProvisioner _provisioner;

    public DynamoDbPaymentBootstrapHostedService(DynamoDbTableProvisioner provisioner)
    {
        _provisioner = provisioner;
    }

    public Task StartAsync(CancellationToken cancellationToken)
        => _provisioner.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
