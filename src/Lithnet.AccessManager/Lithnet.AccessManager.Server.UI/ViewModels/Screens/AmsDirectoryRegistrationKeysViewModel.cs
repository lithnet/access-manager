using System;
using Lithnet.AccessManager.Server.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using MahApps.Metro.SimpleChildWindow;
using Stylet;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsDirectoryRegistrationKeysViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly INotifyModelChangedEventPublisher eventPublisher;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IRegistrationKeyProvider keyProvider;
        private readonly IRegistrationKeyViewModelFactory keyViewModelFactory;
        private readonly ILogger<AmsDirectoryRegistrationKeysViewModel> logger;

      //  public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.KeySolid;

        public AmsDirectoryRegistrationKeysViewModel(IDialogCoordinator dialogCoordinator, INotifyModelChangedEventPublisher eventPublisher, IShellExecuteProvider shellExecuteProvider, IRegistrationKeyProvider keyProvider, IRegistrationKeyViewModelFactory keyViewModelFactory, ILogger<AmsDirectoryRegistrationKeysViewModel> logger)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.eventPublisher = eventPublisher;
            this.shellExecuteProvider = shellExecuteProvider;
            this.keyProvider = keyProvider;
            this.keyViewModelFactory = keyViewModelFactory;
            this.logger = logger;

            this.DisplayName = "Registration keys";
            this.RegistrationKeys = new BindableCollection<RegistrationKeyViewModel>();
        }

        protected override void OnInitialActivate()
        {
            Task.Run(async () =>
            {
                try
                {
                    await foreach (IRegistrationKey m in this.keyProvider.GetRegistrationKeys())
                    {
                        this.RegistrationKeys.Add(keyViewModelFactory.CreateViewModel(m));
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Unable to populate the key list");
                }

                this.eventPublisher.Register(this);
            });
        }

        public BindableCollection<RegistrationKeyViewModel> RegistrationKeys { get; }

        public RegistrationKeyViewModel SelectedRegistrationKey { get; set; }

        public async Task Add()
        {
            DialogWindow w = new DialogWindow
            {
                Title = "Add registration key",
                SaveButtonIsDefault = true
            };

            var m = await this.keyProvider.CreateRegistrationKey();

            var vm = this.keyViewModelFactory.CreateViewModel(m);
            w.DataContext = vm;

            await this.GetWindow().ShowChildWindowAsync(w);

            if (w.Result == MessageDialogResult.Affirmative)
            {
                await this.keyProvider.UpdateRegistrationKey(m);
                this.RegistrationKeys.Add(vm);
            }
        }

        public bool CanEdit => this.SelectedRegistrationKey != null;

        public async Task Edit()
        {
            var selectedKey = this.SelectedRegistrationKey;

            if (selectedKey == null)
            {
                return;
            }

            DialogWindow w = new DialogWindow
            {
                Title = "Edit registration key",
                SaveButtonIsDefault = true
            };

            var m = await this.keyProvider.CloneRegistrationKey(selectedKey.Model);
            var vm = this.keyViewModelFactory.CreateViewModel(m);

            w.DataContext = vm;

            await this.GetWindow().ShowChildWindowAsync(w);

            if (w.Result == MessageDialogResult.Affirmative)
            {
                selectedKey.ActivationCount = vm.ActivationCount;
                selectedKey.ActivationLimit = vm.ActivationLimit;
                selectedKey.Enabled = vm.Enabled;
                selectedKey.Key = vm.Key;
                selectedKey.Name = vm.Name;
                selectedKey.ApprovalRequired = vm.ApprovalRequired;
                await this.keyProvider.UpdateRegistrationKey(selectedKey.Model);
            }
        }

        public bool CanDelete => this.SelectedRegistrationKey != null;

        public async Task Delete()
        {
            MetroDialogSettings s = new MetroDialogSettings
            {
                AnimateShow = false,
                AnimateHide = false
            };

            if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", "Are you sure you want to delete this key?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
            {
                var deleting = this.SelectedRegistrationKey;
                await this.keyProvider.DeleteRegistrationKey(deleting.Model);
                this.RegistrationKeys.Remove(deleting);
                this.SelectedRegistrationKey = this.RegistrationKeys.FirstOrDefault();
            }
        }

        public string HelpLink => Constants.HelpLinkPageJitAccess;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
