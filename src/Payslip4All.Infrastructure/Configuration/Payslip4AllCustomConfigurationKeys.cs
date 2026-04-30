namespace Payslip4All.Infrastructure.Configuration;

public static class Payslip4AllCustomConfigurationKeys
{
    public const string PersistenceProvider = "PERSISTENCE_PROVIDER";
    public const string DefaultConnectionString = "ConnectionStrings:DefaultConnection";
    public const string MySqlConnectionString = "ConnectionStrings:MySqlConnection";
    public const string AuthCookieExpireDays = "Auth:Cookie:ExpireDays";

    public static class DynamoDb
    {
        public const string Region = "DYNAMODB_REGION";
        public const string Endpoint = "DYNAMODB_ENDPOINT";
        public const string TablePrefix = "DYNAMODB_TABLE_PREFIX";
        public const string EnablePointInTimeRecovery = "DYNAMODB_ENABLE_PITR";
        public const string AccessKeyId = "AWS_ACCESS_KEY_ID";
        public const string SecretAccessKey = "AWS_SECRET_ACCESS_KEY";
    }

    public static class ReverseProxy
    {
        public const string Enabled = "REVERSE_PROXY_ENABLED";
        public const string PublicHost = "REVERSE_PROXY_PUBLIC_HOST";
        public const string UpstreamBaseUrl = "REVERSE_PROXY_UPSTREAM_BASE_URL";
        public const string ActivityTimeoutSeconds = "REVERSE_PROXY_ACTIVITY_TIMEOUT_SECONDS";
        public const string CertificatePath = "Kestrel:Certificates:Default:Path";
        public const string CertificatePassword = "Kestrel:Certificates:Default:Password";
    }
}
