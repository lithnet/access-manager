using Lithnet.AccessManager.Server.Providers;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class AmsGroupSelectorViewModel : Screen, IExternalDialogAware
    {
        private readonly ILogger<AmsGroupSelectorViewModel> logger;
        private readonly IAmsGroupProvider groupProvider;
        private readonly IViewModelFactory<AmsGroupViewModel, IAmsGroup> factory;

        public AmsGroupSelectorViewModel(ILogger<AmsGroupSelectorViewModel> logger, IModelValidator<AmsGroupSelectorViewModel> validator, IAmsGroupProvider groupProvider, IViewModelFactory<AmsGroupViewModel, IAmsGroup> factory) : base(validator)
        {
            this.logger = logger;
            this.groupProvider = groupProvider;
            this.factory = factory;
            this.Groups = new BindableCollection<AmsGroupViewModel>();
            this.Validate();
            this.DisplayName = "Select an AMS group";
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
                    this.SelectedItem = this.Groups.FirstOrDefault();
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

        public bool CancelButtonVisible { get; set; } = true;

        public bool SaveButtonVisible { get; set; } = true;

        public bool CancelButtonIsDefault { get; set; } = false;

        public bool SaveButtonIsDefault { get; set; } = true;

        public string SaveButtonName { get; set; } = "Select...";

        public string CancelButtonName { get; set; } = "Cancel";
    }
}
