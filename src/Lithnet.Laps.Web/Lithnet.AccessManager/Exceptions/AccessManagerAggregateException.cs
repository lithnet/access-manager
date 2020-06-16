using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Lithnet.AccessManager
{
    [Serializable]
    public class AccessManagerAggregateException : AggregateException
    {
        public AccessManagerAggregateException()
        {
        }

        public AccessManagerAggregateException(IEnumerable<Exception> innerExceptions) : base(innerExceptions)
        {
        }

        public AccessManagerAggregateException(params Exception[] innerExceptions) : base(innerExceptions)
        {
        }

        public AccessManagerAggregateException(string message) : base(message)
        {
        }

        public AccessManagerAggregateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public AccessManagerAggregateException(string message, IEnumerable<Exception> innerExceptions) : base(message, innerExceptions)
        {
        }

        public AccessManagerAggregateException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public AccessManagerAggregateException(string message, params Exception[] innerExceptions) : base(message, innerExceptions)
        {
        }
    }
}