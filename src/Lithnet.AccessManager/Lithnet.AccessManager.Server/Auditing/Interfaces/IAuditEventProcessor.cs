namespace Lithnet.AccessManager.Server.Auditing
{
    public interface IAuditEventProcessor
    {
        void GenerateAuditEvent(AuditableAction action);
    }
}