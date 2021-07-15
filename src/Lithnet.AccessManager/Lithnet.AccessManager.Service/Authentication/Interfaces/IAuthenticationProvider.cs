using Microsoft.Extensions.DependencyInjection;

namespace Lithnet.AccessManager.Service.AppSettings
{
    public interface IAuthenticationProvider
    {
        bool CanLogout { get;  }

        bool IdpLogout { get; }

        IActiveDirectoryUser GetLoggedInUser();

        void Configure(IServiceCollection services);
    }
}