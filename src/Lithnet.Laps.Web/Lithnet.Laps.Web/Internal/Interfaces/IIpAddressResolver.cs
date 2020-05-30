using System.Web;

namespace Lithnet.Laps.Web.Internal
{
    public interface IIpAddressResolver
    {
        string GetRequestIP(HttpRequestBase request);

        string GetRequestIP(HttpRequest request);
    }
}