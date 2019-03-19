using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices.AccountManagement;
using System.Linq;
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

            // Configure dependency injection.
            // FIXME: There might be a better place to do this.
            UnityConfig.Container.RegisterInstance<LapsConfigSection>(LapsConfigSection.GetConfiguration());
            UnityConfig.Container.RegisterInstance<Logger>(LogManager.GetCurrentClassLogger());
        }

        public static bool CanLogout => Startup.CanLogout && (HttpContext.Current?.User?.Identity?.IsAuthenticated ?? false);
    }
}
