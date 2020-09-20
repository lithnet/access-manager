using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using FluentValidation;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using NLog.Extensions.Logging;
using Stylet;
using StyletIoC;

namespace Lithnet.AccessManager.Server.UI
{
    public class Bootstrapper : Bootstrapper<MainWindowViewModel>
    {
        private readonly ILogger logger;

        private readonly ILoggerFactory loggerFactory;

        private IApplicationConfig appconfig;

        public Bootstrapper()
        {
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddNLog();
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddDebug();
                builder.AddEventLog(new EventLogSettings()
                {
                    SourceName = Constants.EventSourceName,
                    LogName = Constants.EventLogName,
                    Filter = (x, y) => y >= LogLevel.Warning
                });
            });

            logger = loggerFactory.CreateLogger<Bootstrapper>();
        }

        protected override void OnStart()
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            base.OnStart();
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            this.HandleException(e.Exception);
        }

        private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            this.HandleException(e.ExceptionObject as Exception ?? new Exception("An unhandled exception occurred in the app domain, but no exception was present"));
        }

        protected override void OnUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            this.HandleException(e.Exception);
        }

        private void HandleException(Exception ex)
        {
            logger.LogCritical(ex, "An unhandled exception occurred in the user interface");

            string errorMessage = $"An unhandled error occurred and the application will terminate. Do you want to attempt to save the current configuration?";

            if (MessageBox.Show(errorMessage, "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
            {
                try
                {
                    File.Copy(appconfig.Path, appconfig.Path + ".backup", true);
                    appconfig?.Save(appconfig.Path);
                }
                catch (Exception ex2)
                {
                    logger.LogCritical(ex2, "Unable to save app config");
                    MessageBox.Show("Unable to save the current configuration");
                }
            }

            Environment.Exit(1);
        }

        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            IAppPathProvider pathProvider = new AppPathProvider();

            try
            {
                appconfig = ApplicationConfig.Load(pathProvider.ConfigFile);
                var hosting = HostingOptions.Load(pathProvider.HostingConfigFile);
                
                //Config
                builder.Bind<IApplicationConfig>().ToInstance(appconfig);
                builder.Bind<AuthenticationOptions>().ToInstance(appconfig.Authentication);
                builder.Bind<AuditOptions>().ToInstance(appconfig.Auditing);
                builder.Bind<AuthorizationOptions>().ToInstance(appconfig.Authorization);
                builder.Bind<EmailOptions>().ToInstance(appconfig.Email);
                builder.Bind<ForwardedHeadersAppOptions>().ToInstance(appconfig.ForwardedHeaders);
                builder.Bind<HostingOptions>().ToInstance(hosting);
                builder.Bind<RateLimitOptions>().ToInstance(appconfig.RateLimits);
                builder.Bind<UserInterfaceOptions>().ToInstance(appconfig.UserInterface);
                builder.Bind<JitConfigurationOptions>().ToInstance(appconfig.JitConfiguration);

                // ViewModels
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
                builder.Bind<HelpViewModel>().ToSelf();
                builder.Bind<LapsConfigurationViewModel>().ToSelf();
                builder.Bind<JitConfigurationViewModel>().ToSelf();
                builder.Bind<BitLockerViewModel>().ToSelf();

                // ViewModel factories
                builder.Bind<INotificationChannelSelectionViewModelFactory>().To<NotificationChannelSelectionViewModelFactory>();
                builder.Bind<ISecurityDescriptorTargetViewModelFactory>().To<SecurityDescriptorTargetViewModelFactory>();
                builder.Bind<ISecurityDescriptorTargetsViewModelFactory>().To<SecurityDescriptorTargetsViewModelFactory>();
                builder.Bind<IFileSelectionViewModelFactory>().To<FileSelectionViewModelFactory>();
                builder.Bind<IActiveDirectoryDomainPermissionViewModelFactory>().To<ActiveDirectoryDomainPermissionViewModelFactory>();
                builder.Bind<IActiveDirectoryForestSchemaViewModelFactory>().To<ActiveDirectoryForestSchemaViewModelFactory>();
                builder.Bind<IX509Certificate2ViewModelFactory>().To<X509Certificate2ViewModelFactory>();
                builder.Bind<IJitGroupMappingViewModelFactory>().To<JitGroupMappingViewModelFactory>();
                builder.Bind<IJitDomainStatusViewModelFactory>().To<JitDomainStatusViewModelFactory>();
                builder.Bind<IImportTargetsViewModelFactory>().To<ImportTargetsViewModelFactory>();
                builder.Bind(typeof(INotificationChannelDefinitionsViewModelFactory<,>)).ToAllImplementations();
                builder.Bind(typeof(INotificationChannelDefinitionViewModelFactory<,>)).ToAllImplementations();

                // Services
                builder.Bind<RandomNumberGenerator>().ToInstance(RandomNumberGenerator.Create());
                builder.Bind<IDialogCoordinator>().To<DialogCoordinator>();
                builder.Bind<IDirectory>().To<ActiveDirectory>();
                builder.Bind<ILocalSam>().To<LocalSam>();
                builder.Bind<IComputerPrincipalProviderRpc>().To<ComputerPrincipalProviderRpc>();
                builder.Bind<IComputerPrincipalProviderCsv>().To<ComputerPrincipalProviderCsv>();
                builder.Bind<IComputerPrincipalProviderMsLaps>().To<ComputerPrincipalProviderMsLaps>();
                builder.Bind<IComputerPrincipalProviderBitLocker>().To<ComputerPrincipalProviderBitLocker>();
                builder.Bind<IAuthorizationRuleImportProvider>().To<AuthorizationRuleImportProvider>();
                builder.Bind<IDiscoveryServices>().To<DiscoveryServices>();
                builder.Bind<IServiceSettingsProvider>().To<ServiceSettingsProvider>();
                builder.Bind<INotificationSubscriptionProvider>().To<NotificationSubscriptionProvider>();
                builder.Bind<IEncryptionProvider>().To<EncryptionProvider>();
                builder.Bind<ICertificateProvider>().To<CertificateProvider>();
                builder.Bind<IAppPathProvider>().To<AppPathProvider>();
                builder.Bind<INotifyModelChangedEventPublisher>().To<NotifyModelChangedEventPublisher>();
                builder.Bind<IShellExecuteProvider>().To<ShellExecuteProvider>();
                builder.Bind<IDomainTrustProvider>().To<DomainTrustProvider>();
                builder.Bind(typeof(IModelValidator<>)).To(typeof(FluentModelValidator<>));
                builder.Bind(typeof(IValidator<>)).ToAllImplementations();
                builder.Bind<ILoggerFactory>().ToInstance(this.loggerFactory);
                builder.Bind(typeof(ILogger<>)).To(typeof(Logger<>));

                base.ConfigureIoC(builder);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIInitializationError, ex, "Initialization error");
                throw;
            }
        }
    }
}
