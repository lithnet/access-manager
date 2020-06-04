using System;
using NLog;

namespace Lithnet.Laps.Web.Internal
{
    public interface IAuditEventProcessor
    {
        void GenerateAuditEvent(AuditableAction action);
    }
}