using Lithnet.AccessManager.Server.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public class IwaAuthenticationProvider : HttpContextAuthenticationProvider, IIwaAuthenticationProvider
    {
        private readonly IwaAuthenticationProviderOptions options;

        public IwaAuthenticationProvider(IOptions<IwaAuthenticationProviderOptions> options, IDirectory directory, IHttpContextAccessor httpContextAccessor)
            :base (httpContextAccessor, directory)
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