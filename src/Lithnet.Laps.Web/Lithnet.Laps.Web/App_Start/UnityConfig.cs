using System;
using System.Configuration;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Mail;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.Security.Authentication;
using Lithnet.Laps.Web.Security.Authorization;
using Lithnet.Laps.Web.Security.Authorization.ConfigurationFile;
using Microsoft.Practices.Unity.Configuration;
using NLog;
using Unity;
using Unity.NLog;

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

            container.RegisterFactory<ILogger>(_ => LogManager.GetCurrentClassLogger());

            // If no container registrations are found in the config file, then load the defaults here
            if (!container.IsRegistered<IAuthorizationService>())
            {
                container.RegisterType<IAuthorizationService, ConfigurationFileAuthorizationService>();
            }

            if (!container.IsRegistered<IDirectory>())
            {
                container.RegisterType<IDirectory, ActiveDirectory.ActiveDirectory>();
            }

            if (!container.IsRegistered<IAuthenticationService>())
            {
                container.RegisterType<IAuthenticationService, AuthenticationService>();
            }

            if (!container.IsRegistered<IAvailableTargets>())
            {
                container.RegisterType<IAvailableTargets, AvailableTargets>();
            }

            if (!container.IsRegistered<IAvailableReaders>())
            {
                container.RegisterType<IAvailableReaders, AvailableReaders>();
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