using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Service
{
    public class HttpRedirectHostStartup
    {
        public HttpRedirectHostStartup()
        {
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHttpsRedirection();
        }
    }
}
