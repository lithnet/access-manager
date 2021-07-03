namespace Lithnet.AccessManager.Agent
{
    public interface IPasswordChangeProvider
    {
        void ChangePassword(string password);

        string GetAccountName();

        void EnsureEnabled();
    }
}