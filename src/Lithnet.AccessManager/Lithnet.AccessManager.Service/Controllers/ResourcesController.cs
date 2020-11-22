using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Service.Controllers
{
    public class ResourcesController : Controller
    {
        private readonly IAppPathProvider appPathprovider;
        private readonly ILogger logger;

        public ResourcesController(IAppPathProvider appPathprovider, ILogger<ResourcesController> logger)
        {
            this.appPathprovider = appPathprovider;
            this.logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Logo()
        {
            try
            {
                var image = await System.IO.File.ReadAllBytesAsync(appPathprovider.LogoPath);
                return File(image, "image/png");
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.ResourceReadError, ex ,"The image resource could not be loaded");
                return this.NotFound();
            }
        }
    }
}