using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectTypeList //OBJECT_TYPE_LIST
    {
        ObjectTypeLevel Level;
        int Sbz;
        IntPtr ObjectType;
    };
}
