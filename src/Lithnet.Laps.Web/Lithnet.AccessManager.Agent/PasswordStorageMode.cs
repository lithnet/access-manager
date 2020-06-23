using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    [Flags]
    public enum PasswordStorageMode
    {
        None = 0,
        AppData = 1,
        Laps = 2,
    }
}
