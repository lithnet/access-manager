namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public interface IComputerPrincipalProviderCsv : IComputerPrincipalProvider
    {
        void ImportPrincipalMappings(string file, bool hasHeaderRow);

        void ClearPrincipalMappings();
    }
}