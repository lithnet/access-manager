namespace Lithnet.AccessManager.Server
{
    public interface IRegistryProvider
    {
        string LogPath { get; }

        int RetentionDays { get; }

        bool IsConfigured { get; set; }

        string HttpAcl { get; set; }

        string HttpsAcl { get; set; }

        string CertBinding { get; set; }

        string ConfigPath { get; }
        
        string BasePath { get; }

        string ServiceKeyThumbprint { get; set; }
        int CacheMode { get; set; }
        bool DeleteLocalDbInstance { get; set; }
    }
}