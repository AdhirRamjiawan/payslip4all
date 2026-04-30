using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Payslip4All.Web.Tests.Infrastructure;

internal sealed class TestTlsCertificate : IDisposable
{
    private TestTlsCertificate(string certificatePath, string password)
    {
        CertificatePath = certificatePath;
        Password = password;
    }

    public string CertificatePath { get; }

    public string Password { get; }

    public static TestTlsCertificate Create()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=payslip4all.co.za", ecdsa, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(14));

        var certificatePath = Path.Combine(Path.GetTempPath(), $"p4a-gateway-{Guid.NewGuid():N}.pfx");
        var password = $"p4a-{Guid.NewGuid():N}";
        File.WriteAllBytes(certificatePath, certificate.Export(X509ContentType.Pfx, password));

        return new TestTlsCertificate(certificatePath, password);
    }

    public void Dispose()
    {
        if (File.Exists(CertificatePath))
            File.Delete(CertificatePath);
    }
}
