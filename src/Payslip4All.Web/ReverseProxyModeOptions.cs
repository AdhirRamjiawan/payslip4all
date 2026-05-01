using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Payslip4All.Infrastructure.Configuration;

internal sealed class ReverseProxyModeOptions
{
    private const string CertificateActivationError =
        "HTTPS activation failed for payslip4all.co.za: certificate material is missing or invalid; public traffic remains disabled.";
    private const string AspNetCoreUrlsConfigurationKey = "ASPNETCORE_URLS";
    private const string ServerUrlsConfigurationKey = "URLS";

    public bool Enabled { get; init; }
    public string PublicHost { get; init; } = "payslip4all.co.za";
    public string UpstreamBaseUrl { get; init; } = "http://127.0.0.1:8080";
    public string? CertificatePath { get; init; }
    public string? CertificatePassword { get; init; }
    public bool HttpsListenerConfigured { get; init; } = true;
    public int ActivityTimeoutSeconds { get; init; } = 10;

    public static string ActivationErrorMessage => CertificateActivationError;

    public static ReverseProxyModeOptions FromConfiguration(IConfiguration configuration)
    {
        return new ReverseProxyModeOptions
        {
            Enabled = configuration.GetValue<bool>(Payslip4AllCustomConfigurationKeys.ReverseProxy.Enabled),
            PublicHost = Normalize(configuration[Payslip4AllCustomConfigurationKeys.ReverseProxy.PublicHost]) ?? "payslip4all.co.za",
            UpstreamBaseUrl = Normalize(configuration[Payslip4AllCustomConfigurationKeys.ReverseProxy.UpstreamBaseUrl]) ?? "http://127.0.0.1:8080",
            CertificatePath = Normalize(configuration[Payslip4AllCustomConfigurationKeys.ReverseProxy.CertificatePath]),
            CertificatePassword = Normalize(configuration[Payslip4AllCustomConfigurationKeys.ReverseProxy.CertificatePassword]),
            HttpsListenerConfigured = IsHttpsListenerConfigured(
                Normalize(configuration[AspNetCoreUrlsConfigurationKey])
                ?? Normalize(configuration[ServerUrlsConfigurationKey])),
            ActivityTimeoutSeconds = configuration.GetValue<int?>(Payslip4AllCustomConfigurationKeys.ReverseProxy.ActivityTimeoutSeconds) ?? 10
        };
    }

    public void ValidateForStartup()
    {
        if (string.IsNullOrWhiteSpace(PublicHost))
        {
            throw new InvalidOperationException(
                $"{Payslip4AllCustomConfigurationKeys.ReverseProxy.PublicHost} must be set when {Payslip4AllCustomConfigurationKeys.ReverseProxy.Enabled}=true.");
        }

        if (!Uri.TryCreate(UpstreamBaseUrl, UriKind.Absolute, out var upstreamUri)
            || (upstreamUri.Scheme != Uri.UriSchemeHttp && upstreamUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"{Payslip4AllCustomConfigurationKeys.ReverseProxy.UpstreamBaseUrl} must be an absolute http:// or https:// URL when {Payslip4AllCustomConfigurationKeys.ReverseProxy.Enabled}=true.");
        }

        if (!IsInternalOnlyUpstream(upstreamUri))
        {
            throw new InvalidOperationException(
                $"{Payslip4AllCustomConfigurationKeys.ReverseProxy.UpstreamBaseUrl} must target an internal-only upstream endpoint when {Payslip4AllCustomConfigurationKeys.ReverseProxy.Enabled}=true.");
        }

        if (ActivityTimeoutSeconds is <= 0 or > 10)
        {
            throw new InvalidOperationException(
                $"{Payslip4AllCustomConfigurationKeys.ReverseProxy.ActivityTimeoutSeconds} must be between 1 and 10 seconds when {Payslip4AllCustomConfigurationKeys.ReverseProxy.Enabled}=true.");
        }

        if (HttpsListenerConfigured)
            ValidateCertificateMaterial();
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void ValidateCertificateMaterial()
    {
        if (string.IsNullOrWhiteSpace(CertificatePath)
            || string.IsNullOrWhiteSpace(CertificatePassword)
            || !File.Exists(CertificatePath))
        {
            throw new InvalidOperationException(CertificateActivationError);
        }

        try
        {
            _ = new X509Certificate2(CertificatePath, CertificatePassword);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException(CertificateActivationError);
        }
    }

    private static bool IsInternalOnlyUpstream(Uri upstreamUri)
    {
        if (upstreamUri.IsLoopback)
            return true;

        if (IPAddress.TryParse(upstreamUri.Host, out var ipAddress))
        {
            if (IPAddress.IsLoopback(ipAddress))
                return true;

            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                return ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal;

            var bytes = ipAddress.GetAddressBytes();
            return bytes[0] switch
            {
                10 => true,
                172 when bytes[1] is >= 16 and <= 31 => true,
                192 when bytes[1] == 168 => true,
                _ => false
            };
        }

        return upstreamUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || upstreamUri.Host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)
            || upstreamUri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || upstreamUri.Host.EndsWith(".lan", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHttpsListenerConfigured(string? listenUrls)
    {
        if (string.IsNullOrWhiteSpace(listenUrls))
            return true;

        foreach (var url in listenUrls.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
