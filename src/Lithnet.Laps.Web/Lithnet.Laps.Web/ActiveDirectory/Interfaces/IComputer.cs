namespace Lithnet.Laps.Web.ActiveDirectory
{
    public interface IComputer : ISecurityPrincipal
    {
        string Description { get; }

        string DisplayName { get; }
    }
}
