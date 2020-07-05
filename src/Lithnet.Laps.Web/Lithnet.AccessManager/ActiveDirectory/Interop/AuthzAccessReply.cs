using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AuthzAccessReply // AUTHZ_ACCESS_REPLY
    {
        public int ResultListLength;
        public IntPtr GrantedAccessMask;
        public IntPtr SaclEvaluationResults;
        public IntPtr Error;
    }
}
