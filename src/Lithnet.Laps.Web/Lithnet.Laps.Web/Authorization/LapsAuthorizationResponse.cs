using System;
using System.Collections.Generic;

namespace Lithnet.Laps.Web.Authorization
{
    public class LapsAuthorizationResponse : AuthorizationResponse
    {
        /// <summary>
        /// If the user was successfully authorized, then this TimeSpan will be used to determine the new expiry date of the LAPS password. If it is set to zero, then no alteration to the LAPS password expiry date will be made.
        /// </summary>
        public TimeSpan ExpireAfter { get; set; }

        internal override AccessMask EvaluatedAccess => AccessMask.Laps;
    }
}