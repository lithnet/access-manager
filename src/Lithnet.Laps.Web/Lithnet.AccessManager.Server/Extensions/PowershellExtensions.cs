using System.Management.Automation;
using System.Text;
using Lithnet.AccessManager.Server.Exceptions;

namespace Lithnet.AccessManager.Server.Extensions
{
    public static class PowershellExtensions
    {
        public static void ThrowOnPipelineError(this PowerShell powershell)
        {
            if (!powershell.HadErrors)
            {
                return;
            }

            StringBuilder b = new StringBuilder();

            foreach (ErrorRecord error in powershell.Streams.Error)
            {
                if (error.ErrorDetails != null)
                {
                    b.AppendLine(error.ErrorDetails.Message);
                    b.AppendLine(error.ErrorDetails.RecommendedAction);
                }

                b.AppendLine(error.ScriptStackTrace);

                if (error.Exception != null)
                {
                    b.AppendLine(error.Exception.ToString());
                }
            }

            throw new PowerShellScriptException("The PowerShell script encountered an error\n" + b.ToString());
        }

        public static void ResetState(this PowerShell powershell)
        {
            powershell.Streams.ClearStreams();
            powershell.Commands.Clear();
        }
    }
}