using System;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class BitLockerAuthorizationResponse : AuthorizationResponse
    {
        public override AccessMask EvaluatedAccess { get => AccessMask.BitLocker; }
    }
}