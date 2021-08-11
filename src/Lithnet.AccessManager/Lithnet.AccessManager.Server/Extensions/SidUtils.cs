using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Graph;

namespace Lithnet.AccessManager.Server
{
    public static class SidUtils
    {
        public const string AadSidPrefix = "S-1-12-1-";
        public const string AmsSidPrefix = "S-1-4096-";
        public const string AmsObjectSidPrefix = "S-1-4096-1-";
        public const string AmsBuiltInSidPrefix = "S-1-4096-2-";

        public static bool IsAmsBuiltInSid(string sid)
        {
            return sid.StartsWith(AmsBuiltInSidPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAmsObjectSid(string sid)
        {
            return sid.StartsWith(AmsObjectSidPrefix, StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsAmsSid(string sid)
        {
            return sid.StartsWith(AmsSidPrefix, StringComparison.OrdinalIgnoreCase);
        }
        
        public static string ToAmsSidString(this Guid guid)
        {
            return $"{AmsObjectSidPrefix}{guid.ToSidString()}";
        }

        public static string ToAadSidString(this Guid guid)
        {
            return $"{AadSidPrefix}{guid.ToSidString()}";
        }
        
        public static string ToSidString(this Guid guid)
        {
            byte[] bytes = guid.ToByteArray();
            uint[] intar = new uint[4];

            Buffer.BlockCopy(bytes, 0, intar, 0, 16);

            return string.Join('-', intar.Select(t => t.ToString()));
        }

        public static string GuidStringToSidString(string sguid)
        {
            Guid guid = Guid.Parse(sguid);
            return guid.ToSidString();
        }

        public static string GetSidString(this Group g)
        {
            if (g.SecurityIdentifier != null)
            {
                return g.SecurityIdentifier;
            }

            return $"{AadSidPrefix}{SidUtils.GuidStringToSidString(g.Id)}";
        }

        public static SecurityIdentifier GetSid(this Group g)
        {
            return new SecurityIdentifier(g.GetSidString());
        }

        public static string GetSidString(this Device d)
        {
            return $"{AadSidPrefix}{SidUtils.GuidStringToSidString(d.Id)}";
        }

        public static SecurityIdentifier GetSid(this Device d)
        {
            return new SecurityIdentifier(d.GetSidString());
        }

        public static bool IsAmsAuthority(this SecurityIdentifier s)
        {
            return s.Value.StartsWith(AmsSidPrefix);
        }

        public static bool IsAadAuthority(this SecurityIdentifier s)
        {
            return s.Value.StartsWith(AadSidPrefix);
        }

        public static bool IsWindowsAuthority(this SecurityIdentifier s)
        {
            return !s.IsAadAuthority() && !s.IsAmsAuthority();
        }
    }
}
