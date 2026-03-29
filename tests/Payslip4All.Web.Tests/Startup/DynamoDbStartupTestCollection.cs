namespace Payslip4All.Web.Tests.Startup;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DynamoDbStartupTestCollection
{
    public const string Name = "DynamoDB startup";
}
