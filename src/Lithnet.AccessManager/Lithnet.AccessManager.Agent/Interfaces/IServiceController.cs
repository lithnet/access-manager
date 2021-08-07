namespace Lithnet.AccessManager.Agent.Providers
{
    public interface IServiceController
    {
        void RestartService();

        void InstallService();

        void EnableService();
    }
}