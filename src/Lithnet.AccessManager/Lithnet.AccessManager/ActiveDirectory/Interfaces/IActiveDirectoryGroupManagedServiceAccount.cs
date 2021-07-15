namespace Lithnet.AccessManager
{
    public interface IActiveDirectoryGroupManagedServiceAccount : IActiveDirectorySecurityPrincipal
    {
        string DisplayName { get; }
        
        string Description { get; }
        
        string GivenName { get; }

        string Surname { get; }
    }
}
