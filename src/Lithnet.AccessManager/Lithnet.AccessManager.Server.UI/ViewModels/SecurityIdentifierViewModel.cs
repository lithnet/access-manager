using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.ViewModels
{
    public class SecurityIdentifierViewModel
    {
        public string DisplayName { get; set; }

        public string Sid { get; set; }
    }
}
