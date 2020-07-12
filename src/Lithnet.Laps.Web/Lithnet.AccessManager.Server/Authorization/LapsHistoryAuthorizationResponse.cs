using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Authorization
{
    public class LapsHistoryAuthorizationResponse : AuthorizationResponse
    {
        public override AccessMask EvaluatedAccess => AccessMask.LapsHistory;
    }
}