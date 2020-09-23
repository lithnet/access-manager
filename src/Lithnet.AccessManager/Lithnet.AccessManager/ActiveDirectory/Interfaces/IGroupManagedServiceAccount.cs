namespace Lithnet.AccessManager
{
    public interface IGroupManagedServiceAccount : ISecurityPrincipal
    {
        string DisplayName { get; }
        
        string Description { get; }
        
        string GivenName { get; }

        string Surname { get; }
    }
}
