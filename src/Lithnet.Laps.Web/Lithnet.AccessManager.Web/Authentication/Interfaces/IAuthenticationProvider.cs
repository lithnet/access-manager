using Microsoft.Extensions.DependencyInjection;

namespace Lithnet.AccessManager.Web.AppSettings
{
    public interface IAuthenticationProvider
    {
        bool CanLogout { get;  }

        bool IdpLogout { get; }

        IUser GetLoggedInUser();

        void Configure(IServiceCollection services);
    }
}