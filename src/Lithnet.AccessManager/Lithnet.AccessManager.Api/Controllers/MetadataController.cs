using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("agent/metadata")]
    [Authorize("ComputersOnly", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MetadataController : Controller
    {
        public IActionResult Index()
        {
            return this.Json(new
            {
                passwordManagement = new
                {
                    schemes = new[] { "ad", "aad", "psk" }
                }, 
                passwordEncryptionCertificate = "ABC123"
            });
        }
    }
}
