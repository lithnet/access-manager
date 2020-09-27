using System.Threading.Tasks;
using Lithnet.AccessManager.Server.UI.AuthorizationRuleImport;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IImportResultsViewModelFactory
    {
        Task<ImportResultsViewModel> CreateViewModelAsync(ImportResults model);
    }
}