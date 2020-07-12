using System;
using Lithnet.AccessManager.Server;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class LapsHistoryAuthorizationResponse : AuthorizationResponse
    {
        internal override AccessMask EvaluatedAccess => AccessMask.LapsHistory;
    }
}