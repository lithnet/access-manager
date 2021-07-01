using Lithnet.AccessManager.Api.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("auth/iwa")]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = HttpSysDefaults.AuthenticationScheme)]
    public class AuthIwaController : Controller
    {
        private readonly ISecurityTokenGenerator tokenGenerator;
        private readonly IDirectory directory;
        private readonly IDeviceProvider devices;
        private readonly IDiscoveryServices discoveryServices;
        private readonly ILogger<AuthIwaController> logger;
        private readonly IOptions<ApiAuthenticationOptions> agentOptions;
        private readonly IApiErrorResponseProvider errorProvider;

        public AuthIwaController(ISecurityTokenGenerator tokenGenerator, IDirectory directory, IDeviceProvider devices, IDiscoveryServices discoveryServices, ILogger<AuthIwaController> logger, IOptions<ApiAuthenticationOptions> agentOptions, IApiErrorResponseProvider errorProvider)
        {
            this.tokenGenerator = tokenGenerator;
            this.directory = directory;
            this.devices = devices;
            this.discoveryServices = discoveryServices;
            this.logger = logger;
            this.agentOptions = agentOptions;
            this.errorProvider = errorProvider;
        }

        public async Task<IActionResult> PerformIwaAuthAsync()
        {
            try
            {
                if (!this.agentOptions.Value.AllowWindowsAuth)
                {
                    this.logger.LogWarning("A client attempted to authenticate with Windows Authentication, but IWA is currently disabled");
                    throw new UnsupportedAuthenticationTypeException();
                }

                string sid;

                if (//System.Diagnostics.Debugger.IsAttached &&
                    (this.HttpContext.User.FindFirstValue(ClaimTypes.PrimarySid) == "S-1-5-21-3482447370-1165031573-3465620234-1149" ||
                    this.HttpContext.User.FindFirstValue(ClaimTypes.PrimarySid) == "S-1-5-21-3482447370-1165031573-3465620234-1115"))
                {
                    sid = "S-1-5-21-3482447370-1165031573-3465620234-1154"; // substitute out test user SID
                }
                else
                {
                    sid = this.HttpContext.User.FindFirstValue(ClaimTypes.PrimarySid);
                }

                if (sid == null)
                {
                    throw new BadRequestException("The primary SID was missing from the token");
                }

                if (!this.directory.TryGetComputer(sid, out IActiveDirectoryComputer computer))
                {
                    throw new DeviceNotFoundException($"The object with SID {sid} was either not a computer, or could not be found in the domain");
                }

                this.logger.LogTrace($"Attempting to authenticate {computer.MsDsPrincipalName} using IWA");

                IDevice device = await this.devices.GetOrCreateDeviceAsync(computer, this.discoveryServices.GetDomainNameDns(computer.Sid));

                ClaimsIdentity identity = device.ToClaimsIdentity();

                return this.Ok(this.tokenGenerator.GenerateToken(identity));
            }
            catch (Exception ex)
            {
                return this.errorProvider.GetErrorResult(ex);
            }
        }
    }
}
