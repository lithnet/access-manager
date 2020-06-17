using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Lithnet.AccessManager.Interop;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]

    internal struct PolicyAccountDomainInfo
    {
        public LsaUnicodeString DomainName;
        public IntPtr DomainSid;
    }
}
