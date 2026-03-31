namespace Payslip4All.Infrastructure.Tests.DynamoDB;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DynamoDbTestCollection
{
    public const string Name = "DynamoDB tests";
}
