using System;
using NLog;
using System.Management.Automation;
using System.IO;
using Lithnet.Laps.Web.AppSettings;
using System.Text;
using System.Threading.Tasks;
using Lithnet.Laps.Web.ActiveDirectory;
using Newtonsoft.Json;

namespace Lithnet.Laps.Web.Authorization
{
    public class PowershellAuthorizationService : IAuthorizationService
    {
        private readonly ILogger logger;

        private readonly IAuthorizationSettings config;

        private PowerShell powershell;

        public PowershellAuthorizationService(ILogger logger, IAuthorizationSettings config)
        {
            this.logger = logger;
            this.config = config;
        }

        public AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer)
        {
            if (powershell == null)
            {
                this.InitializePowerShellSession();
            }

            this.ResetState();
            this.powershell
                .AddCommand("Get-LapsAuthorizationResponse")
                    .AddParameter("user", user)
                    .AddParameter("computer", computer)
                    .AddParameter("logger", logger);

            Task<AuthorizationResponse> task = new Task<AuthorizationResponse>(() =>
            {
                var results = this.powershell.Invoke();
                this.ThrowOnPipelineError();

                foreach (PSObject result in results)
                {
                    if (result.BaseObject is AuthorizationResponse res)
                    {
                        return res;
                    }
                    else
                    {
                        this.logger.Warn($"The powerShell script returned an unsupported object of type {result.BaseObject?.GetType().FullName} to the pipeline");
                    }
                }

                return null;
            });

            task.Start();
            if(!task.Wait(TimeSpan.FromSeconds(this.config.PowershellScriptTimeout)))
            {
                throw new TimeoutException("The PowerShell script did not complete within the configured time");
            }
            
            if (task.IsFaulted)
            {
                throw task.Exception;
            }

            if (task.Result != null)
            {
                this.logger.Trace($"PowerShell script returned the following AuthorizationResponse: {JsonConvert.SerializeObject(task.Result)}");
                return task.Result;
            }

            this.logger.Warn($"The PowerShell script did not return an AuthorizationResponse");

            return new AuthorizationResponse()
            {
                Code = AuthorizationResponseCode.NoMatchingRuleForComputer,
            };
        }

        private void InitializePowerShellSession()
        {
            string path = System.Web.Hosting.HostingEnvironment.MapPath(this.config.PowershellScriptFile);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"The PowerShell script was not found: {path}");
            }

            powershell = PowerShell.Create();
            powershell.AddScript(File.ReadAllText(path));
            powershell.Invoke();

            if (powershell.Runspace.SessionStateProxy.InvokeCommand.GetCommand("Get-LapsAuthorizationResponse", CommandTypes.All) == null)
            {
                throw new NotSupportedException("The PowerShell script must contain a function called 'Get-LapsAuthorizationResponse'");
            }

            this.logger.Trace($"The PowerShell script was successfully initialized");
        }

        private void ThrowOnPipelineError()
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

        private void ResetState()
        {
            powershell.Streams.ClearStreams();
            powershell.Commands.Clear();
        }
    }
}