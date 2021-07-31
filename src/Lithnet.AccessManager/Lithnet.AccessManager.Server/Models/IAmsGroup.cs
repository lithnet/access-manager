using System.Security.Principal;

namespace Lithnet.AccessManager.Server
{
    public interface IAmsGroup
    {
        long Id { get; set; }

        string Name { get; set; }

        string Description { get; set; }

        string Sid { get; set; }

        AmsGroupType Type { get; set; }

        SecurityIdentifier SecurityIdentifier { get; set; }
    }
}