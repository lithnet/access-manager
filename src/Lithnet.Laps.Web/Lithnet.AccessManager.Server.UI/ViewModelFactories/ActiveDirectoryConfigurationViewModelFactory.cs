namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryConfigurationViewModelFactory : IActiveDirectoryConfigurationViewModelFactory
    {
        private readonly IActiveDirectoryForestConfigurationViewModelFactory forestFactory;

        public ActiveDirectoryConfigurationViewModelFactory(IActiveDirectoryForestConfigurationViewModelFactory forestFactory)
        {
            this.forestFactory = forestFactory;
        }

        public ActiveDirectoryConfigurationViewModel CreateViewModel()
        {
            return new ActiveDirectoryConfigurationViewModel(this.forestFactory);
        }
    }
}
