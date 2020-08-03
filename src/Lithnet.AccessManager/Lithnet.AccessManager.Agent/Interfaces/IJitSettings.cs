using System.Collections.Generic;

namespace Lithnet.AccessManager.Agent
{
    public interface IJitSettings
    {
        string JitGroup { get; }

        IEnumerable<string> AllowedAdmins { get; }

        bool RestrictAdmins { get; }
        
        bool JitEnabled { get; }
    }
}