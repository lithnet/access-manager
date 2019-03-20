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

        Guid? Guid { get; }
        /// <summary>
        /// FIXME: Get rid of this.
        ///
        /// I think this is only used in reporting, and that nobody will use this in reporting.
        /// </summary>
        [Obsolete]
        SecurityIdentifier Sid { get; }
    }
}
