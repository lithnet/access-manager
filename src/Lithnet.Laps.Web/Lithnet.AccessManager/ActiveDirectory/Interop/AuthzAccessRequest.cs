using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AuthzAccessRequest //AUTHZ_ACCESS_REQUEST
    {
        public int DesiredAccess;

        public byte[] PrincipalSelfSid;

        public ObjectTypeList[] ObjectTypeList;

        public int ObjectTypeListLength;

        public IntPtr OptionalArguments;
    }
}
