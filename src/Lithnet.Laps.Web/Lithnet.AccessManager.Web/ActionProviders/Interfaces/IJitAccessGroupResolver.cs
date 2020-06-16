namespace Lithnet.AccessManager.Web
{
    public interface IJitAccessGroupResolver
    {
        IGroup GetJitAccessGroup(IComputer computer, string groupName);
    }
}