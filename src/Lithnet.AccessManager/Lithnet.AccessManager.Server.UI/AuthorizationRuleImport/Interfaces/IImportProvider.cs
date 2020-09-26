using System;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public interface IImportProvider
    {
        event EventHandler<ImportProcessingEventArgs> OnItemProcessStart;

        event EventHandler<ImportProcessingEventArgs> OnItemProcessFinish;

        int GetEstimatedItemCount();
        
        ImportResults Import();
    }
}