using System.IO;
using System.Threading.Tasks;
using Lithnet.AccessManager.Service.App_LocalResources;
using Lithnet.AccessManager.Service.AppSettings;
using Lithnet.AccessManager.Service.Internal;
using Lithnet.AccessManager.Service.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Service.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAuthenticationProvider authSettings;
        private readonly ILogger logger;

        public HomeController(IAuthenticationProvider authSettings, ILogger<HomeController> logger)
        {
            this.authSettings = authSettings;
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return this.RedirectToAction("AccessRequest", "Computer");
        }

        public IActionResult AccessDenied()
        {
            ErrorModel model = new ErrorModel();

            logger.LogTrace($"AuthZ error from {this.Request.HttpContext.Connection.RemoteIpAddress}");

            model.Heading = UIMessages.AccessDenied;
            model.Message = UIMessages.NotAuthorizedMessage;
            return this.View("Error", model);

        }

        public IActionResult AuthNError(AuthNFailureMessageID messageID)
        {
            ErrorModel model = new ErrorModel();

            logger.LogTrace($"AuthN error from {this.Request.HttpContext.Connection.RemoteIpAddress}");

            switch (messageID)
            {
                case AuthNFailureMessageID.SsoIdentityNotFound:
                    model.Heading = UIMessages.AccessDenied;
                    model.Message = UIMessages.SsoIdentityNotFound;
                    break;

                case AuthNFailureMessageID.ExternalAuthNProviderDenied:
                    model.Heading = UIMessages.AccessDenied;
                    model.Message = UIMessages.ExternalAuthNAccessDenied;
                    break;

                case AuthNFailureMessageID.InvalidCertificate:
                    model.Heading = UIMessages.AccessDenied;
                    model.Message = UIMessages.InvalidCertificate;
                    break;

                case AuthNFailureMessageID.UnknownFailure:
                case AuthNFailureMessageID.ExternalAuthNProviderError:
                default:
                    model.Heading = UIMessages.AuthNError;
                    model.Message = UIMessages.UnexpectedError;
                    break;
            }

            return this.View("Error", model);
        }

        public async Task Login(string returnUrl = "/")
        {
            await HttpContext.ChallengeAsync("laps", new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        public async Task<IActionResult> Logout()
        {
            if (this.Request.HttpContext.User.Identity.IsAuthenticated)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                if (authSettings.IdpLogout)
                {
                    return SignOut("laps");
                }
            }

            return this.RedirectToAction("LoggedOut", "Home");
        }

        public IActionResult LoggedOut()
        {
            return this.View();
        }
    }
}