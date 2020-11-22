using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace Lithnet.AccessManager.Server.UI
{
    public class ScriptTemplateProvider : IScriptTemplateProvider
    {
        private readonly EmbeddedFileProvider embeddedProvider;

        public ScriptTemplateProvider()
        {
            this.embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly(), "Lithnet.AccessManager.Server.UI.ScriptTemplates");
        }

        public string AddDomainMembershipPermissions => this.GetFileContent("AddDomainGroupMembershipPermissions.ps1");

        public string CreateGmsa => this.GetFileContent("CreateGmsa.ps1");

        public string EnablePamFeature => this.GetFileContent("EnablePamFeature.ps1");

        public string GrantAccessManagerPermissions => this.GetFileContent("GrantAccessManagerPermissions.ps1");

        public string GrantBitLockerRecoveryPasswordPermissions => this.GetFileContent("GrantBitLockerRecoveryPasswordPermissions.ps1");

        public string GrantGroupPermissions => this.GetFileContent("GrantGroupPermissions.ps1");

        public string GrantMsLapsComputerSelfPermission => this.GetFileContent("GrantMsLapsComputerSelfPermission.ps1");

        public string GrantMsLapsPermissions => this.GetFileContent("GrantMsLapsPermissions.ps1");

        public string PreventDelegation => this.GetFileContent("PreventDelegation.ps1");

        public string PublishLithnetAccessManagerCertificate => this.GetFileContent("PublishLithnetAccessManagerCertificate.ps1");

        public string UpdateAdSchema => this.GetFileContent("UpdateAdSchema.ps1");

        public string WriteAuditLog => this.GetFileContent("WriteAuditLog.ps1");

        public string GetAuthorizationResponse => this.GetFileContent("GetAuthorizationResponse.ps1");
        
        public string CreateDatabase => this.GetFileContent("CreateNewDatabaseScript.sql");

        public string GetFileContent(string filename)
        {
            using (var reader = new StreamReader(embeddedProvider.GetFileInfo(filename).CreateReadStream()))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
