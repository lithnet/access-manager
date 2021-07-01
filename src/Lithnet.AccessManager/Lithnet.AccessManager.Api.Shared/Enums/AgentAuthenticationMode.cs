using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Api.Shared
{
    public enum AgentAuthenticationMode
    {
        None = 0,
        Iwa = 1,
        Aad = 2,
        Ams = 4,
    }
}
