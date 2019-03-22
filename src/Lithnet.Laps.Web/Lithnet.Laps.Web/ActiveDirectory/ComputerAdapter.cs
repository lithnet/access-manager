using System;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.ActiveDirectory
{
    public sealed class ComputerAdapter: IComputer
    {
        private readonly ComputerPrincipal computerPrincipal;

        public ComputerAdapter(ComputerPrincipal computerPrincipal)
        {
            this.computerPrincipal = computerPrincipal;
        }

        public string SamAccountName => computerPrincipal.SamAccountName;
        public string DistinguishedName => computerPrincipal.DistinguishedName;
        public string Description => computerPrincipal.Description;
        public string DisplayName => computerPrincipal.DisplayName;
        public string Name => computerPrincipal.Name;
        public Guid? Guid => computerPrincipal.Guid;
        public SecurityIdentifier Sid => computerPrincipal.Sid;
    }
}