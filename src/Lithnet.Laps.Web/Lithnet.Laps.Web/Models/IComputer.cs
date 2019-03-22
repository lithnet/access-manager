using System;
using System.Security.Principal;

namespace Lithnet.Laps.Web.Models
{
    public interface IComputer
    {
        string SamAccountName { get; }
        string DistinguishedName { get; }
        string Description { get; }
        string DisplayName { get; }
        string Name { get; }

        Guid? Guid { get; }
        SecurityIdentifier Sid { get; }
    }
}
