using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Interop
{
    [Flags]
    internal enum LsaAccessPolicy : long
    {
        PolicyViewLocalInformation = 0x00000001L,
        PolicyViewAuditInformation = 0x00000002L,
        PolicyGetPrivateInformation = 0x00000004L,
        PolicyTrustAdmin = 0x00000008L,
        PolicyCreateAccount = 0x00000010L,
        PolicyCreateSecret = 0x00000020L,
        PolicyCreatePrivilege = 0x00000040L,
        PolicySetDefaultQuotaLimits = 0x00000080L,
        PolicySetAuditRequirements = 0x00000100L,
        PolicyAuditLogAdmin = 0x00000200L,
        PolicyServerAdmin = 0x00000400L,
        PolicyLookupNames = 0x00000800L,
        PolicyNotification = 0x00001000L
    }
}
