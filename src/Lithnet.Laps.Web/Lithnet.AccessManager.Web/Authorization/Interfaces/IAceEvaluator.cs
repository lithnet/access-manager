namespace Lithnet.AccessManager.Web.Authorization
{
    public interface IAceEvaluator
    {
        bool IsMatchingAce(IAce ace, ISecurityPrincipal user, AccessMask requestedAccess);
    }
}