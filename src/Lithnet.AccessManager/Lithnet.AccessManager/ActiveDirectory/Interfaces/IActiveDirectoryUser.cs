namespace Lithnet.AccessManager
{
    public interface IActiveDirectoryUser : IActiveDirectorySecurityPrincipal
    {
        string DisplayName { get; }

        string UserPrincipalName { get; }

        string Description { get; }

        string EmailAddress { get; }

        string GivenName { get; }

        string Surname { get; }
    }
}
