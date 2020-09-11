using System.ComponentModel;

namespace Lithnet.AccessManager.Server.Configuration
{
    public enum JitDcLocatorMode
    {
        Default = 0,

        LocalDcLocator = 1,

        RemoteDcLocator = 2,

        SiteLookup = 4,
    }
}