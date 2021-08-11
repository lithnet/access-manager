using Lithnet.AccessManager.Server.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsGroupSelectorViewModel : Screen
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ILogger<AmsGroupSelectorViewModel> logger;
        private readonly IAmsGroupProvider groupProvider;
        private readonly IViewModelFactory<AmsGroupViewModel, IAmsGroup> factory;

        public AmsGroupSelectorViewModel(IDialogCoordinator dialogCoordinator, ILogger<AmsGroupSelectorViewModel> logger, IModelValidator<AmsGroupSelectorViewModel> validator, IAmsGroupProvider groupProvider, IViewModelFactory<AmsGroupViewModel, IAmsGroup> factory) : base(validator)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.logger = logger;
            this.groupProvider = groupProvider;
            this.factory = factory;
            this.Groups = new BindableCollection<AmsGroupViewModel>();
            this.Validate();
        }

        protected override void OnInitialActivate()
        {
            Task.Run(async () => await this.Initialize());
        }

        private async Task Initialize()
        {
            try
            {
                await foreach (IAmsGroup m in this.groupProvider.GetGroups())
                {
                    if (!this.ShowBuiltInGroups && m.Type == AmsGroupType.System)
                    {
                        continue;
                    }

                    this.Groups.Add(factory.CreateViewModel(m));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not initialize the view model");
                this.ErrorMessageText = ex.ToString();
                this.ErrorMessageHeaderText = "An initialization error occurred";
            }
        }

        public bool ShowBuiltInGroups { get; set; } = true;

        public string ErrorMessageText { get; set; }

        public string ErrorMessageHeaderText { get; set; }

        public BindableCollection<AmsGroupViewModel> Groups { get; }

        public AmsGroupViewModel SelectedItem { get; set; }
    }
}
