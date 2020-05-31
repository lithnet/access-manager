namespace Lithnet.Laps.Web.AppSettings
{
    public interface IUserInterfaceSettings
    {
        string Title { get; }

        AuditReasonFieldState UserSuppliedReason { get; }
    }
}