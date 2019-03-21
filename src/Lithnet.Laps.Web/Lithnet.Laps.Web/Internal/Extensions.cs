using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Claims;
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


        public static UserPrincipal FindUserPrincipalByClaim(this ClaimsIdentity p, IdentityType identityType, params string[] claimNames)
        {
            foreach (string claimName in claimNames)
            {
                Claim c = p.FindFirst(claimName);
                if (c != null)
                {
                    return UserPrincipal.FindByIdentity(new PrincipalContext(ContextType.Domain), identityType, c.Value);
                }
            }

            return null;
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

            return result.Properties[ActiveDirectory.ActiveDirectory.AttrMsMcsAdmPwd][0]?.ToString();
        }
    }
}