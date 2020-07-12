using System;

namespace Lithnet.AccessManager
{
    [Flags]
    public enum PasswordStorageLocation
    {
        Auto = 0,
        LithnetAttribute = 1,
        MsLapsAttribute = 2,
    }
}
