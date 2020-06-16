using System;

namespace Lithnet.AccessManager.Web
{
    [Serializable]
    public class PowerShellScriptException : AccessManagerException
    {
        public PowerShellScriptException()
        {
        }

        public PowerShellScriptException(string message)
            : base(message)
        {
        }

        public PowerShellScriptException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected PowerShellScriptException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}