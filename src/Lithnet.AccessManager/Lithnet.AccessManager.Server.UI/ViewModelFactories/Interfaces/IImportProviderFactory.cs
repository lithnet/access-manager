using Lithnet.AccessManager.Server.UI.AuthorizationRuleImport;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IImportProviderFactory
    {
        IImportProvider CreateImportProvider(ImportSettings settings);
    }
}