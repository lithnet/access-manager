using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using NLog;
using Unity;

namespace Lithnet.Laps.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Configure dependency injection for the logger.
            // FIXME: There is probably a better way do this.
            // FIXME: In the original project, LogManager.GetCurrentClassLogger() was used,
            // which caused each class to use its proper logger. I'm not sure how I can achieve this
            // with unity dependency injection :-(
            UnityConfig.Container.RegisterInstance<ILogger>(LogManager.GetLogger("laps-web"));
        }

        public static bool CanLogout => Startup.CanLogout && (HttpContext.Current?.User?.Identity?.IsAuthenticated ?? false);
    }
}
