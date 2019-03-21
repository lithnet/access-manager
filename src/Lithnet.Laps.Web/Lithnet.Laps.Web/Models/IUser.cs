using System;
using System.Security.Principal;

namespace Lithnet.Laps.Web.Models
{
    public interface IUser
    {
        string SamAccountName { get; }
        string DistinguishedName { get; }
        SecurityIdentifier Sid { get; }

        // I'm not sure we really need all the properties below.
        // They are now used in Reporting.BuildTokenDictionary.
        string DisplayName { get; }
        string UserPrincipalName { get; }
        string Description { get; }
        string EmailAddress { get; }
        Guid? Guid { get; }
        string GivenName { get; }
        string Surname { get; }
    }
}
