using System;

namespace Lithnet.AccessManager.Web.Models
{
    public class JitDetailsModel
    {
        public string ComputerName { get; private set; }

        public string Username { get; private set; }

        public DateTime? ValidUntil { get; private set; }

        internal JitDetailsModel(string computerName, string username, DateTime? validUntil)
        {
            this.ComputerName = computerName;
            this.ValidUntil = validUntil;
            this.Username = username;
        }
    }
}