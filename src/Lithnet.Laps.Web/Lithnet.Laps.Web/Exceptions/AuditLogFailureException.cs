using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lithnet.Laps.Web.Exceptions
{
    public class AuditLogFailureException : LapsWebAppAggregateException
    {
        public AuditLogFailureException()
        {
        }

        public AuditLogFailureException(IEnumerable<Exception> innerExceptions) : base(innerExceptions)
        {
        }

        public AuditLogFailureException(params Exception[] innerExceptions) : base(innerExceptions)
        {
        }

        public AuditLogFailureException(string message) : base(message)
        {
        }

        public AuditLogFailureException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions)
        {
        }

        public AuditLogFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public AuditLogFailureException(string message, params Exception[] innerExceptions) : base(message, innerExceptions)
        {
        }

        protected AuditLogFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
