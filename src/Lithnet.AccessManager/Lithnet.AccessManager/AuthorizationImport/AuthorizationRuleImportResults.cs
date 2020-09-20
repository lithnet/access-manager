using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager
{
    public class AuthorizationRuleImportResults
    {
        public OUPrincipalMapping MappedOU { get; set; }

        public List<ComputerPrincipalMapping> ComputerErrors { get; set; }
    }
}
