using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace Lithnet.Laps.Web.AppSettings
{
    public interface IIdpAuthenticationProvider : IHttpContextAuthenticationProvider
    {
        Task FindClaimIdentityInDirectoryOrFail<T>(RemoteAuthenticationContext<T> context) where T : AuthenticationSchemeOptions;

        Task HandleAuthNFailed(AccessDeniedContext context);

        Task HandleRemoteFailure(RemoteFailureContext context);
    }
}