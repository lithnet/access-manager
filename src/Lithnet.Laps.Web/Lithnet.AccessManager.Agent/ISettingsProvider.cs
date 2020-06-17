using System.Collections.Generic;

namespace Lithnet.AccessManager.Agent
{
    public interface ISettingsProvider
    {
        bool CreateGroup { get; }

        bool Enabled { get; }

        string GroupCreateOu { get; }

        string GroupNameTemplate { get; }

        string CachedGroupSid { get; set; }

        string CachedGroupName { get; set; }

        int GroupType { get; }

        string JitGroup { get; }

        IEnumerable<string> AllowedAdmins { get; }

        bool RemoveUnmanagedMembers { get; }

        int CheckInterval { get; }

        bool PublishLamObject { get; }

        bool PublishJitGroup { get; }

        bool LapsEnabled { get; }
    }
}