using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace Lithnet.AccessManager.Server
{
    public static class SidUtils
    {
        public const string AadSidPrefix = "S-1-12-1-";
        public const string AmsSidPrefix = "S-1-4096-1-";

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

        public static string GetSidString(this Group group)
        {
            return $"{AadSidPrefix}{SidUtils.GuidStringToSidString(group.Id)}";
        }

        public static SecurityIdentifier GetSid(this Group group)
        {
            return new SecurityIdentifier(group.GetSidString());
        }
    }
}
