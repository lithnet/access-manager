using System;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class JitAuthorizationResponse : AuthorizationResponse
    {
        /// <summary>
        /// If the user was successfully authorized, then this TimeSpan will be used to determine when the access will expiry. If it is set to zero, then the access will never expire.
        /// </summary>
        public TimeSpan ExpireAfter { get; set; }

        /// <summary>
        /// A fully qualified group name that the user must be added to in order to grant the JIT access
        /// </summary>
        public string AuthorizingGroup { get; set; }

        public override AccessMask EvaluatedAccess { get => AccessMask.Jit; }
    }
}