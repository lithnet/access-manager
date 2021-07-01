using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using Lithnet.AccessManager.Agent.Interop;
using Microsoft.Win32.SafeHandles;

namespace Lithnet.AccessManager.Agent.Models
{
    public class WorkplaceJoinContext
    {
        public int SessionId { get; set; }

        public X509Certificate2 Certificate { get; set; }

        public DsRegJoinInfo JoinInfo { get; set; }

        public WindowsIdentity Identity { get; set; }

        public SafeAccessTokenHandle TokenHandle { get; set; }

        public bool HadKey { get; set; }
    }
}
