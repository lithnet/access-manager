using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GenericMapping
    {
        public uint GenericRead;

        public uint GenericWrite;

        public uint GenericExecute;

        public uint GenericAll;

        public GenericMapping(uint read, uint write, uint execute, uint all)
        {
            GenericRead = read;
            GenericWrite = write;
            GenericExecute = execute;
            GenericAll = all;
        }
    }
}