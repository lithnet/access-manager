using System.Web;
using Microsoft.AspNetCore.Http;

namespace Lithnet.Laps.Web.Internal
{
    public interface IIpAddressResolver
    {
        string GetRequestIP(HttpRequest request);
    }
}