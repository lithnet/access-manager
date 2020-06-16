using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lithnet.AccessManager.Web
{
    [Serializable]
    public class LapsWebAppAggregateException : AggregateException
    {
        public LapsWebAppAggregateException()
        {
        }

        public LapsWebAppAggregateException(IEnumerable<Exception> innerExceptions) : base(innerExceptions)
        {
        }

        public LapsWebAppAggregateException(params Exception[] innerExceptions) : base(innerExceptions)
        {
        }

        public LapsWebAppAggregateException(string message) : base(message)
        {
        }

        public LapsWebAppAggregateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public LapsWebAppAggregateException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions)
        {
        }

        public LapsWebAppAggregateException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public LapsWebAppAggregateException(string message, params Exception[] innerExceptions) : base(message, innerExceptions)
        {
        }
    }
}