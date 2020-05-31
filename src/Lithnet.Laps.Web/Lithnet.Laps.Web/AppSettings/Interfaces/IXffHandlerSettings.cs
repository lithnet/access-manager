using System.Collections.Generic;

namespace Lithnet.Laps.Web.AppSettings
{
    public interface IXffHandlerSettings
    {
        int ProxyDepth { get; }

        IEnumerable<string> TrustedProxies { get; }

        string HeaderName { get; }

        XffResolverMode Mode { get; }
    }
}