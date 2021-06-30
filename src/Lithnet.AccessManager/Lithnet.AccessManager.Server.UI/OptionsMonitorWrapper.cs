using System;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Server.UI
{
    public class OptionsMonitorWrapper<T> : IOptionsMonitor<T>
    {
        public OptionsMonitorWrapper(T options)
        {
            this.CurrentValue = options;
        }

        public T Get(string name)
        {
            return this.CurrentValue;
        }

        public IDisposable OnChange(Action<T, string> listener)
        {
            return null;
        }

        public T CurrentValue { get; }
    }
}