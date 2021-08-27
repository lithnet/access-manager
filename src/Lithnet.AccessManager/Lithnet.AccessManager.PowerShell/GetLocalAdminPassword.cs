using Lithnet.AccessManager.Cryptography;
using System.Management.Automation;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager.PowerShell
{
    [Cmdlet(VerbsCommon.Get, "LocalAdminPassword")]
    public class GetLocalAdminPassword : Cmdlet
    {
        [Parameter(Mandatory = true, ParameterSetName = "CertificateFile", Position = 1)]
        [Parameter(Mandatory = true, ParameterSetName = "Default", Position = 1)]
        public string ComputerName { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "CertificateFile", Position = 2)]
        public string PfxCertificateFile { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "CertificateFile", Position = 3)]
        public SecureString PfxCertificateFilePassword { get; set; }

        private readonly ICertificateProvider certificateProvider;
        private readonly IEncryptionProvider encryptionProvider;
        private readonly ILithnetAdminPasswordProvider adminPasswordProvider;
        private readonly IActiveDirectory directory;
        private X509Certificate2 certificate;

        public GetLocalAdminPassword()
        {
            this.certificateProvider = DiServices.GetRequiredService<ICertificateProvider>();
            this.encryptionProvider = DiServices.GetRequiredService<IEncryptionProvider>();
            this.adminPasswordProvider = DiServices.GetRequiredService<ILithnetAdminPasswordProvider>();
            this.directory = DiServices.GetRequiredService<IActiveDirectory>();
        }

        protected override void BeginProcessing()
        {

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
            IActiveDirectoryComputer computer = this.directory.GetComputer(this.ComputerName);

            var item = this.adminPasswordProvider.GetCurrentPassword(computer, null);

            if (item == null)
            {
                this.WriteVerbose("The computer did not have a Lithnet local admin password");
                return;
            }


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
    }
}
