using System.Security.Principal;

namespace Lithnet.AccessManager.Server
{
    public interface IAmsGroup
    {
        long Id { get; }

        string Name { get; set; }

        string Description { get; set; }

        string Sid { get; }

        AmsGroupType Type { get; }

        SecurityIdentifier SecurityIdentifier { get; }
    }
}