using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.HttpSys;

namespace Lithnet.AccessManager.Api.Auth.Controllers
{
    [ApiController]
    [Route("auth/tls")]
    [Authorize(AuthenticationSchemes = CertificateAuthenticationDefaults.AuthenticationScheme)]

    public class AuthTlsController : Controller
    {
        public IActionResult Index()
        {
            return this.Ok("OK!");
        }
    }
}
