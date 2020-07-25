namespace Lithnet.AccessManager.Server.Configuration
{
    public enum ClientCertificateValidationMethod
    {
        AnyTrustedIssuer = 0,
        NtAuthStore = 1,
        SpecificIssuer = 2
    }
}
