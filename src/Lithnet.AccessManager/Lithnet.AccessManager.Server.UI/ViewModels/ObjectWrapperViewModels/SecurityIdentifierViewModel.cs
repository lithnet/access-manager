using System.Security.Principal;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityIdentifierViewModel
    {
        private readonly IActiveDirectory directory;

        public SecurityIdentifierViewModel(string sidString, IActiveDirectory directory)
            : this(new SecurityIdentifier(sidString), directory)
        {
        }

        public SecurityIdentifierViewModel(SecurityIdentifier sid, IActiveDirectory directory)
        {
            this.directory = directory;

            this.SecurityIdentifier = sid;
            this.DisplayName = this.GetSidDisplayName(sid);
            this.Sid = sid.ToString();
        }

        public SecurityIdentifier SecurityIdentifier { get; }

        public string DisplayName { get;  }

        public string Sid { get; }

        private string GetSidDisplayName(SecurityIdentifier sid)
        {
            try
            {
                NTAccount adminGroup = (NTAccount)sid.Translate(typeof(NTAccount));
                return adminGroup.Value;
            }
            catch
            {
                try
                {
                    return this.directory.TranslateName(sid.ToString(), AccessManager.Interop.DsNameFormat.SecurityIdentifier, AccessManager.Interop.DsNameFormat.Nt4Name);
                }
                catch
                {
                    return sid.ToString();
                }
            }
        }
    }
}
