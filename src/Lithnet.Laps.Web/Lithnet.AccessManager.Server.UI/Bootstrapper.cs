using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using Lithnet.AccessManager.Server.Configuration;
using Stylet;
using StyletIoC;

namespace Lithnet.AccessManager.Server.UI
{
    public class Bootstrapper : Bootstrapper<RootViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            builder.Bind<IApplicationConfig>().ToFactory(x => ApplicationConfig.Load(@"D:\dev\git\lithnet\laps-web\src\Lithnet.Laps.Web\Lithnet.AccessManager.Web\appsettings.json"));
          
            base.ConfigureIoC(builder);

        }
    }
}
