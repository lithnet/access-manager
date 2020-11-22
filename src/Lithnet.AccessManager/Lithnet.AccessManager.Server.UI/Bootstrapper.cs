using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using FluentValidation;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Providers;
using Lithnet.AccessManager.Server.UI.AuthorizationRuleImport;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Converters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Options;
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

        private static void SetupNLog()
        {
            RegistryProvider provider = new RegistryProvider(false);

            var configuration = new NLog.Config.LoggingConfiguration();

            var uiLog = new NLog.Targets.FileTarget("access-manager-ui")
            {
                FileName = Path.Combine(provider.LogPath, "access-manager-ui.log"),
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date,
                MaxArchiveFiles = provider.RetentionDays,
                Layout = "${longdate}|${level:uppercase=true:padding=5}|${logger}|${message}${onexception:inner=${newline}${exception:format=ToString}}"
            };

            configuration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, uiLog);

            NLog.LogManager.Configuration = configuration;
        }

        public Bootstrapper()
        {
            SetupNLog();

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

            try
            {
                ClusterProvider provider = new ClusterProvider();

                if (provider.IsClustered && !provider.IsOnActiveNode())
                {
                    throw new ClusterNodeNotActiveException("The AMS service is not active on this cluster node. Please edit the configuration on the currently active node");
                }
            }
            catch (ClusterNodeNotActiveException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(EventIDs.UIGenericError, ex, "Unable to determine cluster node status");
            }
        }

        protected override void OnStart()
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            Dispatcher.CurrentDispatcher.UnhandledException += CurrentDispatcher_UnhandledException;

            base.OnStart();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Dispatcher.CurrentDispatcher.UnhandledException -= CurrentDispatcher_UnhandledException;
            TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException -= AppDomain_UnhandledException;

            base.OnExit(e);
        }
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            RegistryProvider registryProvider = new RegistryProvider(true);
            IAppPathProvider pathProvider = new AppPathProvider(registryProvider);

            try
            {
                if (!File.Exists(pathProvider.ConfigFile))
                {
                    this.logger.LogError(EventIDs.UIGenericError, "Config file was not found at path {path}", pathProvider.ConfigFile);
                    throw new MissingConfigurationException($"The appsettings.config file could not be found at path {pathProvider.ConfigFile}. Please resolve the issue and restart the application");
                }

                if (!File.Exists(pathProvider.HostingConfigFile))
                {
                    this.logger.LogError(EventIDs.UIGenericError, "Apphost file was not found at path {path}", pathProvider.HostingConfigFile);
                    throw new MissingConfigurationException($"The apphost.config file could not be found at path {pathProvider.HostingConfigFile}. Please resolve the issue and restart the application");
                }

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
                builder.Bind<LicensingOptions>().ToInstance(appconfig.Licensing);
                builder.Bind<DatabaseConfigurationOptions>().ToInstance(appconfig.DatabaseConfiguration);
                builder.Bind<DataProtectionOptions>().ToInstance(appconfig.DataProtection);

                // ViewModel factories
                builder.Bind(typeof(INotificationChannelDefinitionsViewModelFactory<,>)).ToAllImplementations();
                builder.Bind(typeof(INotificationChannelDefinitionViewModelFactory<,>)).ToAllImplementations();
                builder.Bind<INotificationChannelSelectionViewModelFactory>().To<NotificationChannelSelectionViewModelFactory>();
                builder.Bind<ISecurityDescriptorTargetViewModelFactory>().To<SecurityDescriptorTargetViewModelFactory>();
                builder.Bind<ISecurityDescriptorTargetsViewModelFactory>().To<SecurityDescriptorTargetsViewModelFactory>();
                builder.Bind<IFileSelectionViewModelFactory>().To<FileSelectionViewModelFactory>();
                builder.Bind<IActiveDirectoryDomainPermissionViewModelFactory>().To<ActiveDirectoryDomainPermissionViewModelFactory>();
                builder.Bind<IActiveDirectoryForestSchemaViewModelFactory>().To<ActiveDirectoryForestSchemaViewModelFactory>();
                builder.Bind<IX509Certificate2ViewModelFactory>().To<X509Certificate2ViewModelFactory>();
                builder.Bind<IJitGroupMappingViewModelFactory>().To<JitGroupMappingViewModelFactory>();
                builder.Bind<IJitDomainStatusViewModelFactory>().To<JitDomainStatusViewModelFactory>();
                builder.Bind<IEffectiveAccessViewModelFactory>().To<EffectiveAccessViewModelFactory>();
                builder.Bind<IImportProviderFactory>().To<ImportProviderFactory>();
                builder.Bind<IImportResultsViewModelFactory>().To<ImportResultsViewModelFactory>();


                // Services
                builder.Bind<RandomNumberGenerator>().ToInstance(RandomNumberGenerator.Create());
                builder.Bind<IDialogCoordinator>().To<DialogCoordinator>();
                builder.Bind<IDirectory>().To<ActiveDirectory>();
                builder.Bind<ILocalSam>().To<LocalSam>();
                builder.Bind<IComputerPrincipalProviderRpc>().To<ComputerPrincipalProviderRpc>();
                builder.Bind<IComputerPrincipalProviderCsv>().To<ComputerPrincipalProviderCsv>();
                builder.Bind<IComputerPrincipalProviderLaps>().To<ComputerPrincipalProviderLaps>();
                builder.Bind<IComputerPrincipalProviderBitLocker>().To<ComputerPrincipalProviderBitLocker>();
                builder.Bind<IDiscoveryServices>().To<DiscoveryServices>();
                builder.Bind<IWindowsServiceProvider>().To<WindowsServiceProvider>();
                builder.Bind<INotificationSubscriptionProvider>().To<NotificationSubscriptionProvider>();
                builder.Bind<IEncryptionProvider>().To<EncryptionProvider>();
                builder.Bind<ICertificateProvider>().To<CertificateProvider>();
                builder.Bind<IAppPathProvider>().To<AppPathProvider>();
                builder.Bind<INotifyModelChangedEventPublisher>().To<NotifyModelChangedEventPublisher>();
                builder.Bind<IShellExecuteProvider>().To<ShellExecuteProvider>();
                builder.Bind<IDomainTrustProvider>().To<DomainTrustProvider>();
                builder.Bind<IComputerTargetProvider>().To<ComputerTargetProvider>();
                builder.Bind<IObjectSelectionProvider>().To<ObjectSelectionProvider>();
                builder.Bind<ITargetDataProvider>().To<TargetDataProvider>();
                builder.Bind<ITargetDataCache>().To<TargetDataCache>();
                builder.Bind<IAuthorizationContextProvider>().To<AuthorizationContextProvider>();
                builder.Bind<IAuthorizationInformationBuilder>().To<AuthorizationInformationBuilder>();
                builder.Bind<IPowerShellSecurityDescriptorGenerator>().To<PowerShellSecurityDescriptorGenerator>();
                builder.Bind<IAuthorizationInformationMemoryCache>().To<AuthorizationInformationMemoryCache>();
                builder.Bind<IPowerShellSessionProvider>().To<CachedPowerShellSessionProvider>();
                builder.Bind<IScriptTemplateProvider>().To<ScriptTemplateProvider>();
                builder.Bind<IRegistryProvider>().ToInstance(registryProvider);
                builder.Bind<ICertificatePermissionProvider>().To<CertificatePermissionProvider>();
                builder.Bind<ICertificateSynchronizationProvider>().To<CertificateSynchronizationProvider>();
                builder.Bind<SqlServerInstanceProvider>().ToSelf();

                builder.Bind<IProtectedSecretProvider>().To<ProtectedSecretProvider>().InSingletonScope();
                builder.Bind<IClusterProvider>().To<ClusterProvider>().InSingletonScope();
                builder.Bind<IProductSettingsProvider>().To<ProductSettingsProvider>().InSingletonScope();
                builder.Bind<ILicenseManager>().To<LicenseManager>().InSingletonScope();
                builder.Bind<ISecretRekeyProvider>().To<SecretRekeyProvider>().InSingletonScope();
                builder.Bind<ILicenseDataProvider>().To<OptionsLicenseDataProvider>().InSingletonScope();

                builder.Bind(typeof(IModelValidator<>)).To(typeof(FluentModelValidator<>));
                builder.Bind(typeof(IValidator<>)).ToAllImplementations();
                builder.Bind<ILoggerFactory>().ToInstance(this.loggerFactory);
                builder.Bind(typeof(ILogger<>)).To(typeof(Logger<>));
                builder.Bind(typeof(IOptions<>)).To(typeof(OptionsWrapper<>)).InSingletonScope();
                builder.Bind(typeof(IOptionsSnapshot<>)).To(typeof(OptionsManager<>));
                builder.Bind(typeof(IOptionsFactory<>)).To(typeof(OptionsFactory<>));
                builder.Bind(typeof(IOptionsMonitorCache<>)).To(typeof(OptionsCache<>)).InSingletonScope();

                base.ConfigureIoC(builder);
            }
            catch (ApplicationInitializationException ex)
            {
                this.logger.LogError(EventIDs.UIInitializationError, ex, "Initialization error");
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIInitializationError, ex, "Initialization error");
                throw new ApplicationInitializationException("The application failed to initialize", ex);
            }
        }


        private void CurrentDispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                this.HandleException(e.Exception);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            this.HandleException(e.Exception);
        }

        private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            this.HandleException(e.ExceptionObject as Exception ?? new Exception("An unhandled exception occurred in the app domain, but no exception was present"));
        }

        private void HandleException(Exception ex)
        {
            logger.LogCritical(ex, "An unhandled exception occurred in the user interface");

            string errorMessage = $"An unhandled error occurred and the application will terminate.\r\n\r\n{ex.Message}\r\n\r\n Do you want to attempt to save the current configuration?";

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
                    MessageBox.Show("Unable to save the current configuration", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            Environment.Exit(1);
        }
    }
}
