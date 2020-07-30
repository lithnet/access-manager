namespace Lithnet.AccessManager.Server.Authorization
{
    public interface IAuthorizationInformationBuilder
    {
        void ClearCache(IUser user, IComputer computer);

        AuthorizationInformation GetAuthorizationInformation(IUser user, IComputer computer);
    }
}