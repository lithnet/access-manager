using System;
using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class PreAuthorizationResponse : AuthorizationResponse
    {
        public PreAuthorizationResponse(AccessMask allowedAccess)
        {
            this.EvaluatedAccess = allowedAccess;
        }

        public override AccessMask EvaluatedAccess { get; }
    }
}