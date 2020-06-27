using System;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class LapsHistoryAuthorizationResponse : AuthorizationResponse
    {
        internal override AccessMask EvaluatedAccess => AccessMask.LapsHistory;
    }
}