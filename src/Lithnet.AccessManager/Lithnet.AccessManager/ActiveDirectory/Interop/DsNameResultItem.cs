using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DsNameResultItem
    {
        public DsNameError Status;

        public string Domain;

        public string Name;
    }
}