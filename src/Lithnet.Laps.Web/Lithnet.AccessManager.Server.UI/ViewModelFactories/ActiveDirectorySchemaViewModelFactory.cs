namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectorySchemaViewModelFactory : IActiveDirectorySchemaViewModelFactory
    {
        private readonly IActiveDirectoryForestConfigurationViewModelFactory forestFactory;

        public ActiveDirectorySchemaViewModelFactory(IActiveDirectoryForestConfigurationViewModelFactory forestFactory)
        {
            this.forestFactory = forestFactory;
        }

        public ActiveDirectorySchemaViewModel CreateViewModel()
        {
            return new ActiveDirectorySchemaViewModel(this.forestFactory);
        }
    }
}
