using Lithnet.AccessManager.Service.App_LocalResources;
using Lithnet.AccessManager.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Lithnet.AccessManager.Service.Controllers
{
    public class StatusCode : Controller
    {
        [AllowAnonymous]
        public IActionResult Index(int code)
        {
            if (code == 403)
            {
                return View("Error", new ErrorModel { Heading = UIMessages.Http403Heading, Message = UIMessages.Http403Message });
            }
            else
            {
                return View("Error", new ErrorModel { Heading = $"{code}", Message = ReasonPhrases.GetReasonPhrase(code) });
            }
        }
    }
}
