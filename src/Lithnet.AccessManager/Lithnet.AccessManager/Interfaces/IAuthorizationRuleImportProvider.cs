using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace Lithnet.AccessManager
{
    public interface IAuthorizationRuleImportProvider
    {
        event EventHandler<ProcessingComputerArgs> OnStartProcessingComputer;

        int GetComputerCount(string startingOU);

        AuthorizationRuleImportResults BuildPrincipalMap(AuthorizationRuleImportSettings settings);

        void WriteReport(AuthorizationRuleImportResults results, string path);
    }
}