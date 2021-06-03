using System;
using Lithnet.AccessManager.Api.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.HttpSys;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("auth/iwa")]
    [Authorize(AuthenticationSchemes = HttpSysDefaults.AuthenticationScheme)]
    public class AuthIwaController : Controller
    {
        private readonly ISecurityTokenGenerator tokenGenerator;
        private readonly IDirectory directory;
        private readonly IDeviceProvider devices;
        private readonly IDiscoveryServices discoveryServices;
        private readonly ILogger<AuthIwaController> logger;

        public AuthIwaController(ISecurityTokenGenerator tokenGenerator, IDirectory directory, IDeviceProvider devices, IDiscoveryServices discoveryServices, ILogger<AuthIwaController> logger)
        {
            this.tokenGenerator = tokenGenerator;
            this.directory = directory;
            this.devices = devices;
            this.discoveryServices = discoveryServices;
            this.logger = logger;
        }

        public async Task<IActionResult> IndexAsync()
        {
            try
            { 
                string sid;

                if (System.Diagnostics.Debugger.IsAttached &&
                    this.HttpContext.User.FindFirstValue(ClaimTypes.PrimarySid) == "S-1-5-21-3482447370-1165031573-3465620234-1149")
                {
                    sid = "S-1-5-21-3482447370-1165031573-3465620234-1154"; // substitute out test user SID
                }
                else
                {
                    sid = this.HttpContext.User.FindFirstValue(ClaimTypes.PrimarySid);
                }

                if (!this.directory.TryGetComputer(sid, out IComputer computer))
                {
                    throw new ObjectNotFoundException($"The object with SID {sid} was either not a computer, or could not be found in the domain");
                }

                this.logger.LogTrace($"Attempting to authenticate {computer.MsDsPrincipalName} using IWA");

                Device device = await this.devices.GetOrCreateDeviceAsync(computer, this.discoveryServices.GetDomainNameDns(computer.Sid));

                ClaimsIdentity identity = device.ToClaimsIdentity();

                string token = this.tokenGenerator.GenerateToken(identity);

                return this.Json(new { access_token = token });
            }
            catch (BadRequestException ex)
            {
                this.logger.LogError(ex, "The request could not be processed due to an input error");
                return this.BadRequest();
            }
            catch (ObjectNotFoundException ex)
            {
                this.logger.LogError(ex, "The device could not be found");
                return this.Unauthorized();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "The request could not be processed");
                throw;
            }
        }
    }
}
