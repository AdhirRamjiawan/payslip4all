using Amazon.DynamoDBv2.Model;

namespace Payslip4All.Infrastructure.Persistence.DynamoDB;

internal static class DynamoDbOwnership
{
    public static string GetRequiredUserId(GetItemResponse response, string entityName, Guid entityId)
    {
        if (!response.IsItemSet)
            throw new InvalidOperationException($"{entityName} {entityId} not found.");

        if (!response.Item.TryGetValue("userId", out var userIdAttribute) ||
            string.IsNullOrWhiteSpace(userIdAttribute.S))
        {
            throw new InvalidOperationException($"{entityName} {entityId} is missing user ownership.");
        }

        return userIdAttribute.S;
    }
}
