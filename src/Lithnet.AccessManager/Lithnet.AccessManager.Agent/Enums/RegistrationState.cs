using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    public enum RegistrationState
    {
        NotRegistered = 0,
        Approved = 1,
        Pending = 2,
        Rejected = 4
    }
}
