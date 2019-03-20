namespace Lithnet.Laps.Web.Models
{
    public interface IUser
    {
        string SamAccountName { get; }
        string DistinguishedName { get; }
    }
}
