using System;
using System.Security.Principal;

namespace Lithnet.Laps.Web.Models
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
