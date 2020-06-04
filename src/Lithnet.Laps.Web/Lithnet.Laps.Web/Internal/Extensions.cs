using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.App_LocalResources;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;
using Microsoft.PowerShell.Commands;

namespace Lithnet.Laps.Web.Internal
{
    internal static class Extensions
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

        public static void ThrowOnPipelineError(this PowerShell powershell)
        {
            if (!powershell.HadErrors)
            {
                return;
            }

            StringBuilder b = new StringBuilder();

            foreach (ErrorRecord error in powershell.Streams.Error)
            {
                if (error.ErrorDetails != null)
                {
                    b.AppendLine(error.ErrorDetails.Message);
                    b.AppendLine(error.ErrorDetails.RecommendedAction);
                }

                b.AppendLine(error.ScriptStackTrace);

                if (error.Exception != null)
                {
                    b.AppendLine(error.Exception.ToString());
                }
            }

            throw new PowerShellScriptException("The PowerShell script encountered an error\n" + b.ToString());
        }

        public static void ResetState(this PowerShell powershell)
        {
            powershell.Streams.ClearStreams();
            powershell.Commands.Clear();
        }

        public static string GetLoggedInUserSid(this HttpContext httpContext)
        {
            if (httpContext?.User == null)
            {
                return null;
            }

            ClaimsPrincipal principal = httpContext.User;

            return principal.FindFirst(ClaimTypes.PrimarySid)?.Value ??
                throw new NotFoundException(string.Format(LogMessages.UserNotFoundInDirectory, httpContext.User.Identity.Name));
        }

        public static IUser GetLoggedInUser(this HttpContext httpContext, IDirectory directory)
        {
            string sid = httpContext.GetLoggedInUserSid();

            return directory.GetUser(sid) ??
                throw new NotFoundException(string.Format(LogMessages.UserNotFoundInDirectory, httpContext.User.Identity.Name));
        }

        public static void ForEach<T>(this IEnumerable<T> e, Action<T> action)
        {
            foreach (T item in e)
            {
                action(item);
            }
        }

        public static IEnumerable<string> GetValuesOrDefault(this IConfiguration config, string key, params string[] defaultValues)
        {
            string value = config[key];

            if (value == null)
            {
                var values = config.GetSection(key)?.GetChildren()?.ToList();
                if (values != null && values.Count > 0)
                {
                    foreach (var item in values)
                    {
                        yield return item.Value;
                    }

                    yield break;
                }
            }

            foreach (string dv in defaultValues ?? new string[] { })
            {
                yield return dv;
            }
        }

        public static TEnum GetValueOrDefault<TEnum>(this IConfiguration config, string key, TEnum defaultValue) where TEnum : struct, Enum
        {
            string value = config[key];

            if (value == null)
            {
                var values = config.GetSection(key)?.GetChildren();
                value = string.Join(',', values.Select(t => t.Value));
            }

            if (Enum.TryParse(typeof(TEnum), value, true, out object result))
            {
                if (result == null)
                {
                    return defaultValue;
                }

                return (TEnum)result;
            }

            return defaultValue;
        }

        public static bool GetValueOrDefault(this IConfiguration config, string key, bool defaultValue)
        {
            string value = config[key];

            if (bool.TryParse(value, out bool result))
            {
                return result;
            }

            return defaultValue;
        }

        public static int GetValueOrDefault(this IConfiguration config, string key, int defaultValue)
        {
            string value = config[key];

            if (int.TryParse(value, out int result))
            {
                return result;
            }

            return defaultValue;
        }

        public static int GetValueOrDefault(this IConfiguration config, string key, int minimumValue, int defaultValue)
        {
            string value = config[key];

            if (int.TryParse(value, out int result))
            {
                if (result < minimumValue)
                {
                    return defaultValue;
                }

                return result;
            }

            return defaultValue;
        }

        public static bool TryParseAsSid(this string s, out SecurityIdentifier sid)
        {
            try
            {
                sid = new SecurityIdentifier(s);
                return true;
            }
            catch
            {
                sid = null;
                return false;
            }
        }

        public static string ToClaimList(this ClaimsIdentity p)
        {
            StringBuilder builder = new StringBuilder();
            foreach (Claim c in p.Claims)
            {
                builder.Append(c.Type).Append(": ").AppendLine(c.Value);
            }

            return builder.ToString();
        }

        public static DateTime? GetPropertyDateTimeFromLong(this SearchResult result, string propertyName)
        {
            if (!result.Properties.Contains(propertyName))
            {
                return null;
            }

            long value = (long)result.Properties[propertyName][0];
            return DateTime.FromFileTimeUtc(value).ToLocalTime();
        }

        public static string GetPropertyString(this SearchResult result, string propertyName)
        {
            if (!result.Properties.Contains(propertyName))
            {
                return null;
            }

            return result.Properties[propertyName][0]?.ToString();
        }

        public static string GetPropertyCommaSeparatedString(this SearchResult result, string propertyName)
        {
            if (!result.Properties.Contains(propertyName))
            {
                return null;
            }

            return string.Join(", ", result.Properties[propertyName].OfType<object>().Select(t => t.ToString()));
        }

        public static bool HasPropertyValue(this SearchResult result, string propertyName, string value)
        {
            if (!result.Properties.Contains(propertyName))
            {
                return false;
            }

            foreach (object s in result.Properties[propertyName])
            {
                if (s.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static Guid? GetPropertyGuid(this SearchResult result, string propertyName)
        {
            if (!result.Properties.Contains(propertyName))
            {
                return null;
            }

            byte[] r = Extensions.GetPropertyBytes(result, propertyName);

            if (r == null)
            {
                return null;
            }

            return new Guid(r);
        }

        public static SecurityIdentifier GetPropertySid(this SearchResult result, string propertyName)
        {
            if (!result.Properties.Contains(propertyName))
            {
                return null;
            }

            byte[] r = Extensions.GetPropertyBytes(result, propertyName);

            if (r == null)
            {
                return null;
            }

            return new SecurityIdentifier(r, 0);
        }

        public static byte[] GetPropertyBytes(this SearchResult result, string propertyName)
        {
            if (!result.Properties.Contains(propertyName))
            {
                return null;
            }

            object r = result.Properties[propertyName][0];

            return r as byte[];
        }

        internal static string ToOctetString(this Guid? guid)
        {
            if (!guid.HasValue)
            {
                return null;
            }

            return $"\\{string.Join("\\", guid.Value.ToByteArray().Select(t => t.ToString("X2")))}";
        }

        internal static void AddIfMissing(this StringCollection c, string value, StringComparer comparer)
        {
            if (!c.OfType<string>().Contains(value, comparer))
            {
                c.Add(value);
            }
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

                    options.ClientCertificateMethod = ClientCertificateMethod.AllowRenegotation;
                    options.EnableResponseCaching = false;
                    options.Http503Verbosity = Http503VerbosityLevel.Limited;
                    options.MaxConnections = 100;
                    options.MaxRequestBodySize = 2_048_000;
                    options.MaxAccepts = 0;
                    
                    foreach (string url in config.GetValuesOrDefault("hosting:httpsys:urls"))
                    {
                        options.UrlPrefixes.Add(url);
                    }
                });
            }

            return builder;
        }
    }
}