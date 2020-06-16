namespace Lithnet.AccessManager.Web.Internal
{
    public interface IAuditEventProcessor
    {
        void GenerateAuditEvent(AuditableAction action);
    }
}