using System;
using System.Security.Principal;

namespace Lithnet.Laps.Web.Models
{
    public interface IComputer : ISecurityPrincipal
    {
        string Description { get; }

        string DisplayName { get; }
    }
}
