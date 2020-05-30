using System.Collections.Generic;
using Lithnet.Laps.Web.AppSettings;

namespace Lithnet.Laps.Web
{
    public interface IXffHandling
    {
        int ProxyDepth { get; }

        IEnumerable<string> TrustedProxies { get; }

        string HeaderName { get; }

        XffResolverMode Mode { get; }
    }
}