using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Providers
{
    internal class UiRegistryProvider : RegistryProvider
    {
        public UiRegistryProvider() : base(true)
        {
        }

        public LogLevel UiLogLevel => (LogLevel)(baseKey?.GetValue("UiLogLevel", 0) as int? ?? (int)LogLevel.Information);

        public LogLevel UiEventLogLevel => (LogLevel)(baseKey?.GetValue("UiEventLogLevel", 0) as int? ?? (int)LogLevel.Critical);
    }
}
