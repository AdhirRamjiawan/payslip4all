using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Infrastructure.HostedServices;
using Payslip4All.Infrastructure.HostedPayments;
using Payslip4All.Infrastructure.Persistence.DynamoDB.Repositories;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB;

/// <summary>
/// Extension methods for registering DynamoDB services in the DI container.
/// </summary>
public static class DynamoDbServiceExtensions
{
    /// <summary>
    /// Registers all DynamoDB services: SDK client, repositories, unit of work, and table provisioner.
    /// </summary>
    public static IServiceCollection AddDynamoDbPersistence(this IServiceCollection services, DynamoDbConfigurationOptions options)
    {
        options.ValidateForStartup();

        services.AddSingleton(options);
        services.AddSingleton(sp => new DynamoDbTableNameProvider(sp.GetRequiredService<DynamoDbConfigurationOptions>()));
        services.AddSingleton<IAmazonDynamoDB>(sp => DynamoDbClientFactory.Create(sp.GetRequiredService<DynamoDbConfigurationOptions>()));

        services.AddScoped<IUserRepository, DynamoDbUserRepository>();
        services.AddScoped<ICompanyRepository, DynamoDbCompanyRepository>();
        services.AddScoped<IEmployeeRepository, DynamoDbEmployeeRepository>();
        services.AddScoped<ILoanRepository, DynamoDbLoanRepository>();
        services.AddScoped<IPayslipRepository, DynamoDbPayslipRepository>();
        services.AddScoped<IWalletRepository, DynamoDbWalletRepository>();
        services.AddScoped<IWalletActivityRepository, DynamoDbWalletActivityRepository>();
        services.AddScoped<IPayslipPricingRepository, DynamoDbPayslipPricingRepository>();
        services.AddScoped<IWalletTopUpAttemptRepository, DynamoDbWalletTopUpAttemptRepository>();
        services.AddScoped<IPaymentReturnEvidenceRepository, DynamoDbPaymentReturnEvidenceRepository>();
        services.AddScoped<IOutcomeNormalizationDecisionRepository, DynamoDbOutcomeNormalizationDecisionRepository>();
        services.AddScoped<IUnmatchedPaymentReturnRecordRepository, DynamoDbUnmatchedPaymentReturnRecordRepository>();

        services.AddScoped<IUnitOfWork, DynamoDbUnitOfWork>();

        services.AddSingleton<DynamoDbTableProvisioner>();
        services.AddSingleton<DynamoDbPaymentBootstrapHostedService>();
        services.AddSingleton<DynamoDbBackupProtectionHostedService>();
        services.AddHostedService(sp => sp.GetRequiredService<DynamoDbTableProvisioner>());
        services.AddHostedService(sp => sp.GetRequiredService<DynamoDbPaymentBootstrapHostedService>());
        services.AddHostedService(sp => sp.GetRequiredService<DynamoDbBackupProtectionHostedService>());

        return services;
    }
}
