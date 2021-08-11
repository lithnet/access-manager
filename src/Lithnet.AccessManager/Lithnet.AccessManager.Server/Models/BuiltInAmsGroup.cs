using System;
using System.Security.Principal;

namespace Lithnet.AccessManager.Server.Providers
{
    public class BuiltInAmsGroup : IAmsGroup
    {
        private string name;
        private string description;

        internal BuiltInAmsGroup(SecurityIdentifier sid, string name, string description, Func<IDevice, bool> isIncluded)
        {
            this.name = name;
            this.description = description;
            this.SecurityIdentifier = sid;
            this.Sid = sid.ToString();
            this.Id = 0;
            this.IsIncluded = isIncluded;
        }

        public long Id { get; }

        public string Name
        {
            get => this.name;
            set => throw new NotSupportedException();
        }

        public string Description
        {
            get => this.description;
            set => throw new NotSupportedException();
        }

        public AmsGroupType Type => AmsGroupType.System;

        public string Sid { get; }

        public SecurityIdentifier SecurityIdentifier { get; }

        internal Func<IDevice, bool> IsIncluded { get; }
    }
}