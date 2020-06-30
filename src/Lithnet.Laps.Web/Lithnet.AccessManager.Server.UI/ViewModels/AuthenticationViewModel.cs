using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Lithnet.AccessManager.Configuration;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Microsoft.AspNetCore.Server.HttpSys;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AuthenticationViewModel : PropertyChangedBase, IHaveDisplayName
    {
        private readonly AuthenticationOptions model;

        public AuthenticationViewModel(AuthenticationOptions model)
        {
            this.model = model;
            
            if (model.Iwa == null)
            {
                model.Iwa = new IwaAuthenticationProviderOptions();
            }

            if (model.Oidc == null)
            {
                model.Oidc = new OidcAuthenticationProviderOptions();
            }

            if (model.WsFed == null)
            {
                model.WsFed = new WsFedAuthenticationProviderOptions();
            }
        }

        public AuthenticationMode AuthenticationMode { get => this.model.Mode; set => this.model.Mode = value; }

        public IEnumerable<AuthenticationMode> AuthenticationModeValues
        {
            get
            {
                return Enum.GetValues(typeof(AuthenticationMode)).Cast<AuthenticationMode>();
            }
        }

        public AuthenticationSchemes IwaAuthenticationSchemes { get => this.model.Iwa.AuthenticationSchemes; set => this.model.Iwa.AuthenticationSchemes = value; }

        public IEnumerable<AuthenticationSchemes> IwaAuthenticationSchemesValues
        {
            get
            {
                return Enum.GetValues(typeof(AuthenticationSchemes)).Cast<AuthenticationSchemes>().Where(t => t > 0);
            }
        }

        public bool OidcVisible => this.AuthenticationMode == AuthenticationMode.Oidc;

        public bool WsFedVisible => this.AuthenticationMode == AuthenticationMode.WsFed;

        public bool IwaVisible => this.AuthenticationMode == AuthenticationMode.Iwa;

        public string  OidcAuthority{ get => this.model.Oidc.Authority; set => this.model.Oidc.Authority = value; }

        public string OidcClientID { get => this.model.Oidc.ClientID; set => this.model.Oidc.ClientID = value; }

        public string OidcSecret { get => this.model.Oidc.Secret; set => this.model.Oidc.Secret = value; }

        public string WsFedRealm { get => this.model.WsFed.Realm; set => this.model.WsFed.Realm = value; }

        public string WsFedMetadata { get => this.model.WsFed.Metadata; set => this.model.WsFed.Metadata = value; }

        public string DisplayName { get; set; } = "Authentication";
    }
}
