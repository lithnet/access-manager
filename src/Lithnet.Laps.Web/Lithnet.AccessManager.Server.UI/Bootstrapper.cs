using FluentValidation;
using Lithnet.AccessManager.Configuration;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using StyletIoC;

namespace Lithnet.AccessManager.Server.UI
{
    public class Bootstrapper : Bootstrapper<MainWindowViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            IAppPathProvider pathProvider = new AppPathProvider();

            var appconfig = ApplicationConfig.Load(pathProvider.ConfigFile);

            builder.Bind<IApplicationConfig>().ToInstance(appconfig);
            builder.Bind<AuthenticationOptions>().ToInstance(appconfig.Authentication);
            builder.Bind<AuditOptions>().ToInstance(appconfig.Auditing);
            builder.Bind<AuthorizationOptions>().ToInstance(appconfig.Authorization);
            builder.Bind<EmailOptions>().ToInstance(appconfig.Email);
            builder.Bind<ForwardedHeadersAppOptions>().ToInstance(appconfig.ForwardedHeaders);
            builder.Bind<HostingOptions>().ToInstance(appconfig.Hosting);
            builder.Bind<RateLimitOptions>().ToInstance(appconfig.RateLimits);
            builder.Bind<UserInterfaceOptions>().ToInstance(appconfig.UserInterface);

            builder.Bind<ApplicationConfigViewModel>().ToSelf();
            builder.Bind<AuthenticationViewModel>().ToSelf();
            builder.Bind<EmailViewModel>().ToSelf();
            builder.Bind<HostingViewModel>().ToSelf();
            builder.Bind<AuditingViewModel>().ToSelf();
            builder.Bind<AuthorizationViewModel>().ToSelf();
            builder.Bind<ActiveDirectoryConfigurationViewModel>().ToSelf();

            builder.Bind<IpDetectionViewModel>().ToSelf();
            builder.Bind<PowershellNotificationChannelDefinitionsViewModel>().ToSelf();
            builder.Bind<PowershellNotificationChannelDefinitionViewModel>().ToSelf();
            builder.Bind<RateLimitsViewModel>().ToSelf();
            builder.Bind<SmtpNotificationChannelDefinitionsViewModel>().ToSelf();
            builder.Bind<SmtpNotificationChannelDefinitionViewModel>().ToSelf();

            builder.Bind<UserInterfaceViewModel>().ToSelf();
            builder.Bind<WebhookNotificationChannelDefinitionsViewModel>().ToSelf();
            builder.Bind<WebhookNotificationChannelDefinitionViewModel>().ToSelf();
            
            builder.Bind<IDialogCoordinator>().To<DialogCoordinator>();
            builder.Bind<IDirectory>().To<ActiveDirectory>();
            builder.Bind<IServiceSettingsProvider>().To<ServiceSettingsProvider>();
            builder.Bind<INotificationSubscriptionProvider>().To<NotificationSubscriptionProvider>();
            builder.Bind<IEncryptionProvider>().To<EncryptionProvider>();
            builder.Bind<ICertificateProvider>().To<CertificateProvider>();
            builder.Bind<IAppPathProvider>().To<AppPathProvider>();

            builder.Bind<INotificationChannelSelectionViewModelFactory>().To<NotificationChannelSelectionViewModelFactory>();
            builder.Bind<ISecurityDescriptorTargetViewModelFactory>().To<SecurityDescriptorTargetViewModelFactory>();
            builder.Bind<ISecurityDescriptorTargetsViewModelFactory>().To<SecurityDescriptorTargetsViewModelFactory>();
            builder.Bind<IFileSelectionViewModelFactory>().To<FileSelectionViewModelFactory>();
            builder.Bind<IActiveDirectoryDomainConfigurationViewModelFactory>().To<ActiveDirectoryDomainConfigurationViewModelFactory>();
            builder.Bind<IActiveDirectoryForestConfigurationViewModelFactory>().To<ActiveDirectoryForestConfigurationViewModelFactory>();
            builder.Bind<IX509Certificate2ViewModelFactory>().To<X509Certificate2ViewModelFactory>();

            builder.Bind(typeof(INotificationChannelDefinitionsViewModelFactory<,>)).ToAllImplementations();
            builder.Bind(typeof(INotificationChannelDefinitionViewModelFactory<,>)).ToAllImplementations();

            builder.Bind(typeof(IModelValidator<>)).To(typeof(FluentModelValidator<>));
            builder.Bind(typeof(IValidator<>)).ToAllImplementations();
            builder.Bind<ILoggerFactory>().To<LoggerFactory>();
            builder.Bind(typeof(ILogger<>)).To(typeof(Logger<>));


            base.ConfigureIoC(builder);

        }
    }
}
