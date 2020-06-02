using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.DirectoryServices;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.App_LocalResources;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web.Internal
{
    internal static class Extensions
    {
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

        public static TEnum GetValueOrDefault<TEnum>(this IConfiguration config, string key, TEnum defaultValue) where TEnum : struct, Enum
        {
            string value = config[key];

            if (Enum.TryParse(value, true, out TEnum result))
            {
                return result;
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

        public static IWebHostBuilder UseHttpSysOrIISIntegration(this IWebHostBuilder builder)
        {
            if (builder.GetSetting("hosting:environment") != "iis")
            {
                builder.UseHttpSys(options =>
                {
                    if (builder.GetSetting("authentication:mode") == "iwa")
                    {
                        options.Authentication.Schemes = AuthenticationSchemes.NTLM | AuthenticationSchemes.Negotiate;
                        options.Authentication.AllowAnonymous = false;
                    }
                    else
                    {
                        options.Authentication.AllowAnonymous = true;
                        options.Authentication.Schemes = AuthenticationSchemes.None;
                    }

                    options.MaxConnections = 100;
                    options.MaxRequestBodySize = 2048000;
                    //options.UrlPrefixes.Add("http://localhost:5000");
                    //options.UrlPrefixes.Add("https://localhost:5001");
                });
            }

            return builder;
        }
    }
}