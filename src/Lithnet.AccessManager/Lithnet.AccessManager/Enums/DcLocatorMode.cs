using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager
{
    [Flags]
    public enum DcLocatorMode
    {
        LocalDcLocator = 0,
        RemoteDcLocator = 1,
        SiteLookup = 2,
    }
}
