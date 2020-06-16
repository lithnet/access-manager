namespace Lithnet.AccessManager
{
    public interface IUser : ISecurityPrincipal
    {
        string DisplayName { get; }

        string UserPrincipalName { get; }

        string Description { get; }

        string EmailAddress { get; }

        string GivenName { get; }

        string Surname { get; }
    }
}
