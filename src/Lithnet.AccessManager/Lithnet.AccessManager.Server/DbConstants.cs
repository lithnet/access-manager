using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server
{
    public static class DbConstants
    {
        public const int ErrorDeviceNotFound = 50000;
        public const int ErrorRegistrationKeyNotFound = 50003;
        public const int ErrorRegistrationKeyActivationLimitExceeded = 50004;
        public const int ErrorRegistrationKeyDisabled = 50005;
        public const int ErrorCannotDeleteBuiltInGroup = 50006;

    }
}
