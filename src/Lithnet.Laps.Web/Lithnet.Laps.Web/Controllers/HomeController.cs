using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Lithnet.Laps.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return this.RedirectToAction("Get", "Lap");
        }

        public ActionResult AuthNError()
        {
            return this.View();
        }

        public ActionResult SignOut()
        {
            if (this.Request.GetOwinContext().Authentication.User.Identity.IsAuthenticated)
            {
                this.Request.GetOwinContext()
                    .Authentication
                    .SignOut(this.HttpContext.GetOwinContext()
                        .Authentication.GetAuthenticationTypes()
                        .Select(o => o.AuthenticationType).ToArray());
            }

            return this.View("LogOut");
        }

        public ActionResult LogOut()
        {
            return this.View();
        }
    }
}