using System;

namespace Lithnet.AccessManager.Interop
{
    [Flags]
    internal enum LsaAccessPolicy : uint
    {
        PolicyViewLocalInformation = 0x00000001,
        PolicyViewAuditInformation = 0x00000002,
        PolicyGetPrivateInformation = 0x00000004,
        PolicyTrustAdmin = 0x00000008,
        PolicyCreateAccount = 0x00000010,
        PolicyCreateSecret = 0x00000020,
        PolicyCreatePrivilege = 0x00000040,
        PolicySetDefaultQuotaLimits = 0x00000080,
        PolicySetAuditRequirements = 0x00000100,
        PolicyAuditLogAdmin = 0x00000200,
        PolicyServerAdmin = 0x00000400,
        PolicyLookupNames = 0x00000800,
        PolicyNotification = 0x00001000
    }
}
