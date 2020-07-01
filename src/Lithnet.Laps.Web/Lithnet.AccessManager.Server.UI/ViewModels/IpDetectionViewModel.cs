using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Lithnet.AccessManager.Configuration;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class IpDetectionViewModel : PropertyChangedBase, IHaveDisplayName
    {
        private readonly ForwardedHeadersAppOptions model;

        private readonly IDialogCoordinator dialogCoordinator;

        public IpDetectionViewModel(ForwardedHeadersAppOptions model, IDialogCoordinator dialogCoordinator)
        {
            this.model = model;
            this.dialogCoordinator = dialogCoordinator;
            this.KnownProxies = new BindableCollection<string>(model.KnownProxies);
            this.KnownNetworks = new BindableCollection<string>(model.KnownNetworks);
        }

        public bool Enabled
        {
            get => this.model.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedFor);
            set
            {
                if (value)
                {
                    this.model.ForwardedHeaders |= ForwardedHeaders.XForwardedFor;
                }
                else
                {
                    this.model.ForwardedHeaders &= ~ForwardedHeaders.XForwardedFor;
                }
            }
        }

        public string ForwardedForHeaderName { get => this.model.ForwardedForHeaderName; set => this.model.ForwardedForHeaderName = value; }

        public int? ForwardLimit { get => this.model.ForwardLimit; set => this.model.ForwardLimit = value; }

        public BindableCollection<string> KnownProxies { get; }

        public BindableCollection<string> KnownNetworks { get; }

        public string SelectedNetwork { get; set; }

        public string NewNetwork { get; set; }

        public async Task AddNetwork()
        {
            if (string.IsNullOrWhiteSpace(this.NewNetwork))
            {
                return;
            }

            var split = this.NewNetwork.Split("/");

            if (split.Length != 2)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Validation", "The specified value was not a valid CIDR network range");
                return;
            }

            if (!int.TryParse(split[1], out int mask))
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Validation", "The specified CIDR mask is not valid");
                return;
            }
            else
            {
                if (mask < 0  || mask > 128)
                {
                    await this.dialogCoordinator.ShowMessageAsync(this, "Validation", "The specified CIDR mask is not valid");
                    return;

                }
            }

            if (IPAddress.TryParse(split[0], out _))
            {
                this.KnownNetworks.Add(this.NewNetwork);
            }
            else
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Validation", "The specified value was not a valid IP address");
                return;
            }
        }

        public bool CanAddNetwork()
        {
            return this.Enabled && !string.IsNullOrWhiteSpace(this.NewNetwork);
        }

        public void RemoveNetwork()
        { 
            if (this.SelectedNetwork != null)
            {
                this.KnownNetworks.Remove(this.SelectedNetwork);
            }
        }
        
        public bool CanRemoveNetwork()
        {
            return this.Enabled && this.SelectedNetwork != null;
        }

        public string SelectedProxy { get; set; }

        public string NewProxy { get; set; }

        public async Task AddProxy()
        {
            if (string.IsNullOrWhiteSpace(this.NewProxy))
            {
                return;
            }

            if (IPAddress.TryParse(this.NewProxy, out _))
            {
                this.KnownProxies.Add(this.NewProxy);
            }
            else
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Validation", "The specified value was not a valid IP address");
            }
        }

        public bool CanAddProxy()
        {
            return this.Enabled && !string.IsNullOrWhiteSpace(this.NewProxy);
        }

        public void RemoveProxy()
        {
            if (this.SelectedProxy != null)
            {
                this.KnownProxies.Remove(this.SelectedProxy);
            }
        }

        public bool CanRemoveProxy()
        {
            return this.Enabled && this.SelectedProxy != null;
        }

        public string DisplayName { get; set; } = "IP address detection";
    }
}
