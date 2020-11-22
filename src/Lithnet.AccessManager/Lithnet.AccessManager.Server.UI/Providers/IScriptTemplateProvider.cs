namespace Lithnet.AccessManager.Server.UI
{
    public interface IScriptTemplateProvider
    {
        string AddDomainMembershipPermissions { get; }

        string CreateGmsa { get; }

        string EnablePamFeature { get; }

        string GrantAccessManagerPermissions { get; }

        string GrantBitLockerRecoveryPasswordPermissions { get; }

        string GrantGroupPermissions { get; }

        string GrantMsLapsComputerSelfPermission { get; }

        string GrantMsLapsPermissions { get; }

        string PreventDelegation { get; }

        string PublishLithnetAccessManagerCertificate { get; }

        string UpdateAdSchema { get; }

        string WriteAuditLog { get; }

        string GetAuthorizationResponse { get; }
        string CreateDatabase { get; }

        string GetFileContent(string filename);
    }
}