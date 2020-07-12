namespace Lithnet.AccessManager
{
    public interface IJitAccessGroupResolver
    {
        IGroup GetJitAccessGroup(IComputer computer, string groupName);
    }
}