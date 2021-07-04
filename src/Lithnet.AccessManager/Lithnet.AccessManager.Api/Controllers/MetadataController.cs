using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("agent/metadata")]
    [Produces("application/json")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true)]
    public class MetadataController : Controller
    {
        private readonly IOptions<ApiAuthenticationOptions> agentOptions;
        private readonly IOptions<AzureAdOptions> azureAdOptions;
        private readonly ILogger<MetadataController> logger;
        private readonly IApiErrorResponseProvider errorProvider;

        public MetadataController(IOptions<ApiAuthenticationOptions> agentOptions, ILogger<MetadataController> logger, IApiErrorResponseProvider errorProvider, IOptions<AzureAdOptions> azureAdOptions)
        {
            this.agentOptions = agentOptions;
            this.logger = logger;
            this.errorProvider = errorProvider;
            this.azureAdOptions = azureAdOptions;
        }

        public IActionResult Index()
        {
            try
            {
                return NotFound();

                List<string> allowedOptions = new List<string>();

                if (this.agentOptions.Value.AllowAzureAdJoinedDeviceAuth)
                {
                    allowedOptions.Add("aadj");
                }

                if (this.agentOptions.Value.AllowAzureAdRegisteredDeviceAuth)
                {
                    allowedOptions.Add("aadr");
                }

                if (this.agentOptions.Value.AllowWindowsAuth)
                {
                    allowedOptions.Add("iwa");
                }

                if (this.agentOptions.Value.AllowAmsManagedDeviceAuth)
                {
                    allowedOptions.Add("ssa");
                }

                return this.Json(new MetadataResponse
                {
                    AgentAuthentication = new AgentAuthentication
                    {
                        AllowedOptions = allowedOptions,
                        AllowedAzureAdTenants = this.azureAdOptions.Value.Tenants?.Select(t => t.TenantId).ToList() ?? new List<string>()
                    },
                    PasswordManagement = new PasswordManagement { }
                });

            }
            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
        }
    }
}
