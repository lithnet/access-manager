namespace Lithnet.AccessManager.Server.UI
{
    public interface IAmsGroupMemberViewModel
    {
        object Model { get; }

        string DisplayName { get; }

        string Type { get; }

        string Sid { get; }
    }
}