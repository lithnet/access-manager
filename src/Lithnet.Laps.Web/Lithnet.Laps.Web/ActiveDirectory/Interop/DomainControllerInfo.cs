using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web.ActiveDirectory.Interop
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
