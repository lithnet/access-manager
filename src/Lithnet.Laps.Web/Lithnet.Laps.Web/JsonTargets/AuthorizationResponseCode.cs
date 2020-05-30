using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lithnet.Laps.Web.JsonTargets
{
    public enum AuthorizationResponseCode
    {
        Undefined = 0,
        Success = 1,
        NoMatchingRuleForComputer = 2,
        NoMatchingRuleForUser = 3,
        UserDeniedByAce = 4,
    }
}