using System;
using System.Security.Principal;

namespace Lithnet.Laps.Web.Models
{
    public interface IGroup
    {
        Guid? Guid { get; }
        SecurityIdentifier Sid { get; }
    }
}
