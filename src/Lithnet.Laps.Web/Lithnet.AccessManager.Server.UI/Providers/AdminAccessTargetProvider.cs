using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Text;
using Community.Security.AccessControl;
using Lithnet.AccessManager.Web.Authorization;

namespace Lithnet.AccessManager.Server.UI.Providers
{
    public class AdminAccessTargetProvider : GenericProvider
    {
        public override ResourceType ResourceType => ResourceType.ProviderDefined;

        public override void GetAccessListInfo(ObjInfoFlags flags, out AccessRightInfo[] rights, out uint defaultIndex)
        {
            rights = new AccessRightInfo[] {
                new AccessRightInfo(0x00000200, "Laps access", AccessFlags.General),
                new AccessRightInfo(0x00000400, "Laps history", AccessFlags.General),
                new AccessRightInfo(0x00000800, "Just-in-time access",  AccessFlags.General),
                new AccessRightInfo(0, "Admin access", AccessFlags.Ignore)
            };

            defaultIndex = 0;
        }

        public override GenericMapping GetGenericMapping(sbyte AceFlags)
        {
            return new GenericMapping(0, 0, 0, 0x00000200 | 0x00000500 | 0x00000800);
        }
    }
}
