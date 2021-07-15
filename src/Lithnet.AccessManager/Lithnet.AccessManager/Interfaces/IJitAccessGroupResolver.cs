namespace Lithnet.AccessManager
{
    public interface IJitAccessGroupResolver
    {
        IActiveDirectoryGroup GetJitGroup(IComputer computer, string groupName);

        IActiveDirectoryGroup GetJitGroup(string groupNameTemplate, string computerName, string domain);

        string BuildGroupName(string groupNameTemplate, string computerDomain, string computerName);

        bool IsTemplatedName(string groupNameTemplate);
    }
}