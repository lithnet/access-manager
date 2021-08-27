using Lithnet.AccessManager.Cryptography;
using System;
using System.Management.Automation;
using System.Security;
using System.Security.Cryptography.X509Certificates;

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

        private readonly ICertificateProvider certificateProvider;
        private readonly IEncryptionProvider encryptionProvider;
        private readonly ILithnetAdminPasswordProvider adminPasswordProvider;
        private readonly IActiveDirectory directory;
        private X509Certificate2 certificate;

        public GetLocalAdminPasswordHistory()
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
                    if (!string.IsNullOrWhiteSpace(item.AccountName))
                    {
                        result.Properties.Add(new PSNoteProperty("AccountName", item.AccountName));
                    }

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
