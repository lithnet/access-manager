using System;
using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]

    internal struct PolicyAccountDomainInfo
    {
        public LsaUnicodeString DomainName;
        public IntPtr DomainSid;
    }
}
