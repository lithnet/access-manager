using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using HttpSys = Microsoft.AspNetCore.Server.HttpSys;

namespace Lithnet.AccessManager.Web.Internal
{
    internal static class WebHostExtensions
    {
        public static IWebHostBuilder UseHttpSys(this IWebHostBuilder builder, IConfiguration config)
        {
            if (config.GetValueOrDefault("hosting:environment", HostingEnvironment.HttpSys) == HostingEnvironment.HttpSys)
            {
                HttpSysHostingOptions p = new HttpSysHostingOptions();
                config.Bind("Hosting:HttpSys", p);

                builder.UseHttpSys(options =>
                 {
                     var mode = config.GetValueOrDefault("Authentication:Mode", AuthenticationMode.Iwa);


                     if (mode == AuthenticationMode.Iwa)
                     {
                         options.Authentication.Schemes = config.GetValueOrDefault("Authentication:Iwa:AuthenticationSchemes", HttpSys.AuthenticationSchemes.Negotiate);
                         options.Authentication.AllowAnonymous = false;
                         options.ClientCertificateMethod = HttpSys.ClientCertificateMethod.NoCertificate;
                     }
                     else if (mode == AuthenticationMode.Certificate)
                     {
                         options.Authentication.AllowAnonymous = true;
                         options.Authentication.Schemes = HttpSys.AuthenticationSchemes.None;
                         options.ClientCertificateMethod = HttpSys.ClientCertificateMethod.AllowCertificate;
                     }
                     else
                     {
                         options.Authentication.AllowAnonymous = true;
                         options.Authentication.Schemes = HttpSys.AuthenticationSchemes.None;
                         options.ClientCertificateMethod = HttpSys.ClientCertificateMethod.NoCertificate;
                     }

                     options.AllowSynchronousIO = p.AllowSynchronousIO;
                     options.EnableResponseCaching = p.EnableResponseCaching;
                     options.Http503Verbosity = (HttpSys.Http503VerbosityLevel)p.Http503Verbosity;
                     options.MaxAccepts = p.MaxAccepts;
                     options.MaxConnections = p.MaxConnections;
                     options.MaxRequestBodySize = p.MaxRequestBodySize;
                     options.RequestQueueLimit = p.RequestQueueLimit;
                     options.ThrowWriteExceptions = p.ThrowWriteExceptions;

                     options.UrlPrefixes.Clear();
                     options.UrlPrefixes.Add(p.BuildHttpUrlPrefix());
                     options.UrlPrefixes.Add(p.BuildHttpsUrlPrefix());
                 });
            }

            return builder;
        }
    }
}