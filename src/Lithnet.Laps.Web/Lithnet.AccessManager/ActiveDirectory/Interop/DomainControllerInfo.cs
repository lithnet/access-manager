using System;
using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DomainControllerInfo
    {
        public string DomainControllerName;

        public string DomainControllerAddress;

        public uint DomainControllerAddressType;

        public Guid DomainGuid;
        
        public string DomainName;

        public string DnsForestName;
       
        public DsGetDcNameFlags Flags;
        
        public string DcSiteName;
       
        public string ClientSiteName;
    }
}
