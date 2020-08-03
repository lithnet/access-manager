namespace Lithnet.AccessManager
{
    public interface IJitAccessGroupResolver
    {
        IGroup GetJitGroup(IComputer computer, string groupName);

        IGroup GetJitGroup(string groupNameTemplate, string computerName, string domain);

        string BuildGroupName(string groupNameTemplate, string computerDomain, string computerName);

        bool IsTemplatedName(string groupNameTemplate);
    }
}