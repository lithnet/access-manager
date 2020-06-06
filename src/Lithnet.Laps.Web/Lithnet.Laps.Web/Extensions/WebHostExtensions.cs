using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.Internal
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
            if (config["hosting:environment"] != "iis")
            {
                builder.UseHttpSys(options =>
                {
                    if (config["authentication:mode"] == "iwa")
                    {
                        options.Authentication.Schemes = config.GetValueOrDefault("authentication:iwa:authentication-schemes", AuthenticationSchemes.Negotiate);
                        options.Authentication.AllowAnonymous = false;
                    }
                    else
                    {
                        options.Authentication.AllowAnonymous = true;
                        options.Authentication.Schemes = AuthenticationSchemes.None;
                    }

                    //options.ClientCertificateMethod = ClientCertificateMethod.AllowRenegotation;
                    //options.EnableResponseCaching = false;
                    //options.Http503Verbosity = Http503VerbosityLevel.Limited;
                    //options.MaxConnections = 100;
                    //options.MaxRequestBodySize = 2_048_000;
                    //options.MaxAccepts = 0;

                    var urls = config.GetValuesOrDefault("hosting:httpsys:urls")?.ToList() ?? new List<string>();

                    if (urls.Count == 0)
                    {
                        options.UrlPrefixes.Add("http://+:80");
                        options.UrlPrefixes.Add("https://+:443");
                    }

                    foreach (string url in urls)
                    {
                        options.UrlPrefixes.Add(url);
                    }
                });
            }

            return builder;
        }
    }
}