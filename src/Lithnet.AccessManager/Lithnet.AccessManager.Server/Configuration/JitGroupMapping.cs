namespace Lithnet.AccessManager.Server.Configuration
{
    public class JitGroupMapping
    { 
        public string ComputerOU { get; set; }

        public bool Subtree { get; set; }

        public string GroupOU { get; set; }

        public string GroupNameTemplate { get; set; }

        public GroupType GroupType { get; set; } = GroupType.DomainLocal;

        public string GroupDescription { get; set; }

        public bool EnableJitGroupDeletion { get; set; }

        public string PreferredDC { get; set; }
    }
}