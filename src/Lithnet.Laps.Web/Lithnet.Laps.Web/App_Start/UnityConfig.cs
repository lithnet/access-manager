using System;
using System.Configuration;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Mail;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.Security.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Practices.Unity.Configuration;
using NLog;
using Unity;
using Unity.NLog;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.Internal;
using Lithnet.Laps.Web.Config;
using Lithnet.Laps.Web.JsonTargets;

namespace Lithnet.Laps.Web
{
    /// <summary>
    /// Specifies the Unity configuration for the main container.
    /// </summary>
    public static class UnityConfig
    {
        #region Unity Container
        private static Lazy<IUnityContainer> container =
          new Lazy<IUnityContainer>(() =>
          {
              var container = new UnityContainer();
              container.AddNewExtension<NLogExtension>();
              RegisterTypes(container);
              return container;
          });

        /// <summary>
        /// Configured Unity Container.
        /// </summary>
        public static IUnityContainer Container => container.Value;
        #endregion

        /// <summary>
        /// Registers the type mappings with the Unity container.
        /// </summary>
        /// <param name="container">The unity container to configure.</param>
        /// <remarks>
        /// There is no need to register concrete types such as controllers or
        /// API controllers (unless you want to change the defaults), as Unity
        /// allows resolving a concrete type even if it was not previously
        /// registered.
        /// </remarks>
        public static void RegisterTypes(IUnityContainer container)
        {
            if (((UnityConfigurationSection)ConfigurationManager.GetSection("unity"))?.Containers.Count > 0)
            {
                container.LoadConfiguration();
            }

            var configRoot = new ConfigurationBuilder()
                .AddJsonFile("app_data/appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("app_data/appsecrets.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("laps")
                .Build();

            container.RegisterInstance(configRoot);

            container.RegisterFactory<ILogger>(_ => LogManager.GetCurrentClassLogger());
            container.RegisterType<IOidcSettings, OidcSettings>();
            container.RegisterType<IIwaSettings, IwaSettings>();
            container.RegisterType<IWsFedSettings, WsFedSettings>();
            container.RegisterType<IUserInterfaceSettings, UserInterfaceSettings>();
            container.RegisterType<IRateLimitSettings, RateLimitSettings>();
            container.RegisterType<IAuthenticationSettings, AuthenticationSettings>();
            container.RegisterType<IIpResolverSettings, IpResolverSettings>();
            container.RegisterType<IIpAddressResolver, IpAddressResolver>();
            container.RegisterType<GlobalAuditSettings, GlobalAuditSettings>();
            container.RegisterType<IJsonTargetsProvider, JsonFileTargetsProvider>();
            container.RegisterType<IAuthorizationService, JsonTargetAuthorizationService>();

            // If no container registrations are found in the config file, then load 

            if (!container.IsRegistered<IDirectory>())
            {
                container.RegisterType<IDirectory, ActiveDirectory.ActiveDirectory>();
            }

            if (!container.IsRegistered<IAuthenticationService>())
            {
                container.RegisterType<IAuthenticationService, AuthenticationService>();
            }

            if (!container.IsRegistered<IReporting>())
            {
                container.RegisterType<IReporting, Reporting>();
            }

            if (!container.IsRegistered<ITemplates>())
            {
                container.RegisterType<ITemplates, TemplatesFromFiles>();
            }

            if (!container.IsRegistered<IRateLimiter>())
            {
                container.RegisterType<IRateLimiter, RateLimiter>();
            }

            if (!container.IsRegistered<IMailer>())
            {
                container.RegisterType<IMailer, SmtpMailer>();
            }
        }
    }
}