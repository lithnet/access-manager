using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Service.AppSettings
{
    public class IwaAuthenticationProvider : HttpContextAuthenticationProvider, IIwaAuthenticationProvider
    {
        private readonly IwaAuthenticationProviderOptions options;

        public IwaAuthenticationProvider(IOptions<IwaAuthenticationProviderOptions> options, IActiveDirectory directory, IHttpContextAccessor httpContextAccessor, IAuthorizationContextProvider authzContextProvider, ILogger<IwaAuthenticationProvider> logger)
            : base(httpContextAccessor, directory, authzContextProvider)
        {
            this.options = options.Value;
        }

        public override bool CanLogout => false;

        public override bool IdpLogout => false;

        public override void Configure(IServiceCollection services)
        {
            services.AddAuthentication(HttpSysDefaults.AuthenticationScheme);
        }
    }
}