using System;
using System.DirectoryServices;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Web;

namespace Lithnet.Laps.Web
{
    internal static class Extensions
    {
        public static string GetXffList(this HttpRequest request)
        {
            return request.ServerVariables["HTTP_X_FORWARDED_FOR"];
        }

        public static string GetXffIP(this HttpRequest request)
        {
            return request.GetXffList()?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault();
        }

        public static string GetUnmaskedIP(this HttpRequest request)
        {
            string ip = request.GetXffIP();

            return string.IsNullOrWhiteSpace(ip) ? request.UserHostAddress : ip;
        }

        public static string GetXffList(this HttpRequestBase request)
        {
            return request.ServerVariables["HTTP_X_FORWARDED_FOR"];
        }

        public static string GetXffIP(this HttpRequestBase request)
        {
            return request.GetXffList()?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault();
        }

        public static string GetUnmaskedIP(this HttpRequestBase request)
        {
            string ip = request.GetXffIP();

            return string.IsNullOrWhiteSpace(ip) ? request.UserHostAddress : ip;
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

        public static Guid? GetPropertyGuid(this SearchResult result, string propertyName)
        {
            if (!result.Properties.Contains(propertyName))
            {
                return null;
            }

            byte[] r = GetPropertyBytes(result, propertyName);

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

            byte[] r = GetPropertyBytes(result, propertyName);

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
    }
}