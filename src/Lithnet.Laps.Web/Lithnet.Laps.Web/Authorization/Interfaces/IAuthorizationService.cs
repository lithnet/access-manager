using Lithnet.Laps.Web.ActiveDirectory;

namespace Lithnet.Laps.Web.Authorization
{
    public interface IAuthorizationService
    {
        LapsAuthorizationResponse GetLapsAuthorizationResponse(IUser user, IComputer computer);

        JitAuthorizationResponse GetJitAuthorizationResponse(IUser user, IComputer computer);
    }
}