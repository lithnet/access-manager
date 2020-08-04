using Lithnet.AccessManager.Web.App_LocalResources;
using Lithnet.AccessManager.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Lithnet.AccessManager.Web.Controllers
{
    public class StatusCode : Controller
    {
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
