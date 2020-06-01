using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Lithnet.Laps.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return this.RedirectToAction("Get", "Lap");
        }

        public IActionResult AuthNError()
        {
            return this.View();
        }

        public async Task Login(string returnUrl = "/")
        {
            // specifying the scheme here "oidc"
            await HttpContext.ChallengeAsync("laps", new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        public async Task SignOut()
        {
            await HttpContext.SignOutAsync("laps", new AuthenticationProperties
            {
                RedirectUri = Url.Action("SignOut", "Home")
            });

            //if (this.Request.GetOwinContext().Authentication.User.Identity.IsAuthenticated)
            //{
            //    this.Request.GetOwinContext()
            //        .Authentication
            //        .SignOut(this.HttpContext.GetOwinContext()
            //            .Authentication.GetAuthenticationTypes()
            //            .Select(o => o.AuthenticationType).ToArray());
            //}

            //return this.View("LogOut");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public IActionResult LogOut()
        {
            return this.View();
        }
    }
}