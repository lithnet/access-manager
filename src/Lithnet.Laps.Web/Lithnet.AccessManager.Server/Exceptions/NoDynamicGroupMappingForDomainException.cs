using System;

namespace Lithnet.AccessManager.Server.Exceptions
{
    [Serializable]
    public class NoDynamicGroupMappingForDomainException : AccessManagerException
    {
        public NoDynamicGroupMappingForDomainException()
        {
        }

        public NoDynamicGroupMappingForDomainException(string message)
            : base(message)
        {
        }

        public NoDynamicGroupMappingForDomainException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected NoDynamicGroupMappingForDomainException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}