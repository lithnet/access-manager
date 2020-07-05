using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using FluentValidation;
using Lithnet.AccessManager.Configuration;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Stylet;
using StyletIoC;

namespace Lithnet.AccessManager.Server.UI
{
    public class Bootstrapper : Bootstrapper<MainWindowViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            var appconfig = ApplicationConfig.Load(@"D:\dev\git\lithnet\laps-web\src\Lithnet.Laps.Web\Lithnet.AccessManager.Web\appsettings.json");

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

            builder.Bind<IpDetectionViewModel>().ToSelf();
            builder.Bind<PowershellNotificationChannelDefinitionsViewModel>().ToSelf();
            builder.Bind<PowershellNotificationChannelDefinitionViewModel>().ToSelf();
            builder.Bind<RateLimitsViewModel>().ToSelf();
            builder.Bind<SmtpNotificationChannelDefinitionsViewModel>().ToSelf();
            builder.Bind<SmtpNotificationChannelDefinitionViewModel>().ToSelf();

            builder.Bind<UserInterfaceViewModel>().ToSelf();
            builder.Bind<WebhookNotificationChannelDefinitionsViewModel>().ToSelf();
            builder.Bind<WebhookNotificationChannelDefinitionViewModel>().ToSelf();
            
            builder.Bind<IDialogCoordinator>().To(typeof(DialogCoordinator));
            builder.Bind<INotificationSubscriptionProvider>().To(typeof(NotificationSubscriptionProvider));
            builder.Bind(typeof(IModelValidator<>)).To(typeof(FluentModelValidator<>));
            builder.Bind(typeof(IValidator<>)).ToAllImplementations();

            base.ConfigureIoC(builder);

        }
    }
}
