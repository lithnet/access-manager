using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public interface IXffHandlerSettings
    {
        string ForwardedForHeaderName { get; }

        string ForwardedHostHeaderName { get; }

        string ForwardedProtoHeaderName { get; }

        int ForwardLimit { get; }

        IEnumerable<IPNetwork> TrustedNetworks { get; }

        IEnumerable<IPAddress> TrustedProxies { get; }
    }
}