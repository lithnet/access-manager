using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Lithnet.AccessManager.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateDefaultHost(args).Build().Run();
        }

        public static IHostBuilder CreateDefaultHost(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                    .UseUrls("https://localhost:44385/api/v1.0")
                    .UseHttpSys(x =>
                    {
                        x.Authentication.Schemes = Microsoft.AspNetCore.Server.HttpSys.AuthenticationSchemes.Negotiate | Microsoft.AspNetCore.Server.HttpSys.AuthenticationSchemes.NTLM;
                        x.Authentication.AllowAnonymous = true;
                        x.ClientCertificateMethod = Microsoft.AspNetCore.Server.HttpSys.ClientCertificateMethod.AllowRenegotation;
                    })
                        .UseStartup<ApiCoreStartup>();
                });
    }
}
