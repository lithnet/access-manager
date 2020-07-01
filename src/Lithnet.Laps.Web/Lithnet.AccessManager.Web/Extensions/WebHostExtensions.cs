using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lithnet.AccessManager.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lithnet.AccessManager.Web.Internal
{
    internal static class WebHostExtensions
    {
        public static string ResolvePath(this IWebHostEnvironment env, string path, params string[] searchSegments)
        {
            if (Path.IsPathFullyQualified(path))
            {
                return path;
            }
            else
            {
                string mappedpath = Path.Combine(env.ContentRootPath, $"{path}");

                if (File.Exists(mappedpath))
                {
                    return mappedpath;
                }

                if (searchSegments != null)
                {
                    foreach (string segment in searchSegments)
                    {
                        mappedpath = Path.Combine(env.ContentRootPath, segment, $"{path}");

                        if (File.Exists(mappedpath))
                        {
                            return mappedpath;
                        }
                    }
                }
            }

            return null;
        }

        public static IWebHostBuilder UseHttpSys(this IWebHostBuilder builder, IConfiguration config)
        {
            if (config.GetValueOrDefault("hosting:environment", HostingEnvironment.HttpSys) == HostingEnvironment.HttpSys)
            {
                HttpSysHostingOptions p = new HttpSysHostingOptions();
                config.Bind("Hosting:HttpSys", p);

                builder.UseHttpSys(options =>
                 {
                     if (config.GetValueOrDefault("Authentication:Mode", AuthenticationMode.Iwa) == AuthenticationMode.Iwa)
                     {
                         options.Authentication.Schemes = config.GetValueOrDefault("Authentication:Iwa:AuthenticationSchemes", AuthenticationSchemes.Negotiate);
                         options.Authentication.AllowAnonymous = false;
                     }
                     else
                     {
                         options.Authentication.AllowAnonymous = true;
                         options.Authentication.Schemes = AuthenticationSchemes.None;
                     }

                     options.AllowSynchronousIO = p.AllowSynchronousIO;
                     options.ClientCertificateMethod = p.ClientCertificateMethod;
                     options.EnableResponseCaching = p.EnableResponseCaching;
                     options.Http503Verbosity = p.Http503Verbosity;
                     options.MaxAccepts = p.MaxAccepts;
                     options.MaxConnections = p.MaxConnections;
                     options.MaxRequestBodySize = p.MaxRequestBodySize;
                     options.RequestQueueLimit = p.RequestQueueLimit;
                     options.RequestQueueMode = p.RequestQueueMode;
                     options.RequestQueueName = p.RequestQueueName;
                     options.ThrowWriteExceptions = p.ThrowWriteExceptions;
                     
                     options.UrlPrefixes.Clear();
                     options.UrlPrefixes.Add(p.BuildHttpUrlPrefix());
                     options.UrlPrefixes.Add(p.BuildHttpsUrlPrefix());
                 }
                 );
            }

            return builder;
        }
    }
}