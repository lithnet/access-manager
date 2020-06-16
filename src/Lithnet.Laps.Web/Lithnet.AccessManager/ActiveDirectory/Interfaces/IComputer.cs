namespace Lithnet.AccessManager
{
    public interface IComputer : ISecurityPrincipal
    {
        string Description { get; }

        string DisplayName { get; }
    }
}
