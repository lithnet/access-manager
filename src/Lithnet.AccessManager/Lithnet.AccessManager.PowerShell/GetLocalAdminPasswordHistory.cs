using System;
using System.Management.Automation;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.PowerShell.Cmdletization.Xml;

namespace Lithnet.AccessManager.PowerShell
{
    [Cmdlet(VerbsCommon.Get, "LocalAdminPasswordHistory")]
    public class GetLocalAdminPasswordHistory : Cmdlet
    {
        [Parameter(Mandatory = true, ParameterSetName = "CertificateFile", Position = 1)]
        [Parameter(Mandatory = true, ParameterSetName = "Default", Position = 1)]
        public string ComputerName { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "CertificateFile", Position = 2)]
        public string PfxCertificateFile { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "CertificateFile", Position = 3)]
        public SecureString PfxCertificateFilePassword { get; set; }

        private ILoggerFactory logFactory;
        private IDiscoveryServices discoveryServices;
        private ICertificateProvider certificateProvider;
        private IEncryptionProvider encryptionProvider;
        private ILithnetAdminPasswordProvider adminPasswordProvider;
        private IDirectory directory;
        private X509Certificate2 certificate;

        protected override void BeginProcessing()
        {
            this.logFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            this.discoveryServices = new DiscoveryServices(logFactory.CreateLogger<DiscoveryServices>());
            this.certificateProvider = new CertificateProvider(logFactory.CreateLogger<CertificateProvider>(), discoveryServices);
            this.encryptionProvider = new EncryptionProvider();
            this.adminPasswordProvider = new LithnetAdminPasswordProvider(logFactory.CreateLogger<LithnetAdminPasswordProvider>(), encryptionProvider, certificateProvider);
            this.directory = new ActiveDirectory(discoveryServices);

            if (this.PfxCertificateFile != null)
            {
                this.certificate = new X509Certificate2(this.PfxCertificateFile, this.PfxCertificateFilePassword);
            }
            else
            {
                this.certificate = null;
            }
        }

        protected override void ProcessRecord()
        {
            IComputer computer = this.directory.GetComputer(this.ComputerName);

            var items = this.adminPasswordProvider.GetPasswordHistory(computer);
            
            if (items == null || items.Count == 0)
            {
                this.WriteVerbose("The computer did not have a Lithnet local admin password");
                return;
            }

            foreach (var item in items)
            {
                try
                {
                    var decryptedData = this.encryptionProvider.Decrypt(item.EncryptedData, (thumbprint) =>
                    {
                        if (certificate != null)
                        {
                            if (string.Equals(thumbprint, certificate.Thumbprint, System.StringComparison.OrdinalIgnoreCase))
                            {
                                return certificate;
                            }
                        }

                        return this.certificateProvider.FindDecryptionCertificate(thumbprint);
                    });

                    var result = new PSObject();
                    result.Properties.Add(new PSNoteProperty("ComputerName", computer.MsDsPrincipalName));
                    result.Properties.Add(new PSNoteProperty("Password", decryptedData));
                    result.Properties.Add(new PSNoteProperty("Created", item.Created.ToLocalTime()));
                    result.Properties.Add(new PSNoteProperty("Retired", item.Retired?.ToLocalTime()));

                    this.WriteObject(result);
                }
                catch (Exception ex)
                {
                    this.WriteError(new ErrorRecord(ex, "UnableToDecryptPassword", ErrorCategory.InvalidData, item));
                }
            }
        }
    }
}
