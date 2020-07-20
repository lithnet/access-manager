using System;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    [Flags]
    public enum InheritFlags : uint
    {
        NoInheritance = 0,

        ObjectInheritAce = 1,

        ContainerInheritAce = 2,
        
        NoPropagateInheritAce = 4,

        InheritOnlyAce = 8,

        InheritedAce = 0x10,

        SiAccessSpecific = 0x00010000,

        SiAccessGeneral = 0x00020000,

        SiAccessContainer = 0x00040000,

        SiAccessProperty = 0x00080000,
    }
}