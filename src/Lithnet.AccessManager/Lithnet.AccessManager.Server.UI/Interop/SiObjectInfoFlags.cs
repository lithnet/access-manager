using System;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [Flags]
    public enum SiObjectInfoFlags : uint
    {
        EditPermissions = 0x00000000,

        EditOwner = 0x00000001,
        
        EditAudit = 0x00000002,
        
        Container = 0x00000004,
        
        ReadOnly = 0x00000008,
        
        ShowAdvancedButton = 0x00000010,
        
        Reset = 0x00000020,
        
        OwnerReadonly = 0x00000040,
        
        EditProperties = 0x00000080,
        
        OwnerRecurse = 0x00000100,
        
        NoAclProtect = 0x00000200,
        
        NoTreeApply = 0x00000400,
        
        PageTitle = 0x00000800,
        
        ServerIsDc = 0x00001000,

        ResetDaclTree = 0x00004000,
        
        ResetSaclTree = 0x00008000,

        ObjectGuid = 0x00010000,

        ResetDacl = 0x00040000,
        
        ResetSacl = 0x00080000,

        ShowEffectivePermissions = 0x00020000,
        
        ResetOwner = 0x00100000,

        NoAdditionalPermission = 0x00200000,
        
        ViewOnly = 0x00400000,

        PermissionElevationRequired = 0x01000000,
        
        AuditElevationRequired = 0x02000000,
        
        OwnerElevationRequired = 0x04000000,

        MayWrite = 0x10000000,
    }
}