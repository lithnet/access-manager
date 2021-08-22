using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ExternalDialogWindowViewModelFactory : IViewModelFactory<ExternalDialogWindowViewModel, Screen>
    {
        private readonly IShellExecuteProvider shellExecuteProvider;

        public ExternalDialogWindowViewModelFactory(IShellExecuteProvider shellExecuteProvider)
        {
            this.shellExecuteProvider = shellExecuteProvider;
        }

        public ExternalDialogWindowViewModel CreateViewModel(Screen childViewModel)
        {
            return new ExternalDialogWindowViewModel(childViewModel, shellExecuteProvider);
        }
    }
}
