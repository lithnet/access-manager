using System;
using System.Collections.Generic;
using System.Text;

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
