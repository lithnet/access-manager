using System.Collections.Generic;
using System.Security.Principal;

namespace Lithnet.AccessManager
{
    public interface ILocalSam
    {
        void AddLocalGroupMember(string groupName, SecurityIdentifier member);

        string GetBuiltInAdministratorsGroupName();

        string GetBuiltInAdministratorsGroupNameOrDefault(string server);

        string GetBuiltInAdministratorsGroupName(string server);

        IList<SecurityIdentifier> GetLocalGroupMembers(string name);

        IList<SecurityIdentifier> GetLocalGroupMembers(string server, string name);

        string GetMachineNetbiosDomainName();

        string GetMachineNTAccountName();

        SecurityIdentifier GetWellKnownSid(WellKnownSidType sidType);

        SecurityIdentifier GetWellKnownSid(WellKnownSidType sidType, SecurityIdentifier domainSid);
        
        SecurityIdentifier GetWellKnownSid(string server, WellKnownSidType sidType);
        
        SecurityIdentifier GetLocalMachineAuthoritySid(string server);
        
        bool IsDomainController();

        void RemoveLocalGroupMember(string groupName, SecurityIdentifier member);

        bool UpdateLocalGroupMembership(string groupName, IEnumerable<SecurityIdentifier> membersToAdd, IEnumerable<SecurityIdentifier> membersToRemove, bool ignoreErrors);

        bool UpdateLocalGroupMembership(string groupName, IEnumerable<SecurityIdentifier> allowedMembers, bool allowOthers, bool ignoreErrors);

        void SetLocalAccountPassword(SecurityIdentifier sid, string password);
        string GetBuiltInAdministratorAccountName();
        bool IsDomainJoined();
        void EnableLocalAccount(SecurityIdentifier sid);
    }
}