namespace Lithnet.AccessManager.Server
{
    public interface ICertificateSynchronizationProvider
    {
        void ExportCertificatesToConfig();

        void ImportCertificatesFromConfig();
    }
}