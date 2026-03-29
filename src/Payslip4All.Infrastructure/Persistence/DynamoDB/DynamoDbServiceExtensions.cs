using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Payslip4All.Application.Interfaces;
using Payslip4All.Application.Interfaces.Repositories;
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
    public static IServiceCollection AddDynamoDbPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonDynamoDB>(_ => DynamoDbClientFactory.Create());

        services.AddScoped<IUserRepository, DynamoDbUserRepository>();
        services.AddScoped<ICompanyRepository, DynamoDbCompanyRepository>();
        services.AddScoped<IEmployeeRepository, DynamoDbEmployeeRepository>();
        services.AddScoped<ILoanRepository, DynamoDbLoanRepository>();
        services.AddScoped<IPayslipRepository, DynamoDbPayslipRepository>();

        services.AddScoped<IUnitOfWork, DynamoDbUnitOfWork>();

        services.AddHostedService<DynamoDbTableProvisioner>();

        return services;
    }
}
