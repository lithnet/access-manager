using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Lithnet.AccessManager.Configuration;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class EmailViewModel : PropertyChangedBase, IHaveDisplayName
    {
        private readonly EmailOptions model;

        public EmailViewModel(EmailOptions model)
        {
            this.model = model;
        }

        public string FromAddress { get => this.model.FromAddress; set => this.model.FromAddress = value; }

        public int Port { get => this.model.Port; set => this.model.Port = value; }

        public string Host { get => this.model.Host; set => this.model.Host = value; }

        public bool UseSsl { get => this.model.UseSsl; set => this.model.UseSsl = value; }

        public bool UseSpecifiedCredentials { get => !this.model.UseDefaultCredentials; set => this.model.UseDefaultCredentials = !value; }

        public string Username { get => this.model.Username; set => this.model.Username = value; }

        public string Password { get => this.model.Password; set => this.model.Password = value; }

        public string DisplayName { get; set; } = "Email";
    }
}
