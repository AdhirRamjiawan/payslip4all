using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Payslip4All.Application.Interfaces.Repositories;
using Payslip4All.Domain.Entities;
using Payslip4All.Web.Tests.Startup;

namespace Payslip4All.Web.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(DynamoDbStartupTestCollection.Name)]
public sealed class DynamoDbLocalWorkflowTests
{
    [Fact]
    public async Task CreateClient_WithLocalEndpoint_AllowsRepresentativeUserRepositoryWorkflow()
    {
        using var harness = new LocalDynamoDbTestHarness();
        var email = $"owner-{Guid.NewGuid():N}@example.com";

        await using var factory = harness.CreateFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var dynamoDb = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();

        try
        {
            var user = new User
            {
                Email = email,
                PasswordHash = "hash",
            };

            await userRepository.AddAsync(user);

            var savedUser = await userRepository.GetByEmailAsync(email);

            Assert.NotNull(savedUser);
            Assert.Equal(user.Id, savedUser!.Id);
            Assert.True(await userRepository.ExistsAsync(email));
            Assert.Contains($"{harness.TablePrefix}_users", harness.ExpectedTableNames);
        }
        finally
        {
            await harness.DeleteProvisionedTablesAsync(dynamoDb);
        }
    }
}
