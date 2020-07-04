using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class WebhookNotificationChannelDefinitionsViewModel : PropertyChangedBase, IHaveDisplayName, IViewAware
    {
        private readonly IList<WebhookNotificationChannelDefinition> model;

        private readonly IDialogCoordinator dialogCoordinator;

        public BindableCollection<WebhookNotificationChannelDefinitionViewModel> ViewModels { get; }

        public WebhookNotificationChannelDefinitionsViewModel(IList<WebhookNotificationChannelDefinition> model, IDialogCoordinator dialogCoordinator)
        {
            this.model = model;
            this.dialogCoordinator = dialogCoordinator;
            this.ViewModels = new BindableCollection<WebhookNotificationChannelDefinitionViewModel>(model.Select(t => new WebhookNotificationChannelDefinitionViewModel(t)));
        }
 
        public string DisplayName { get; set; } = "Webhook";

        public WebhookNotificationChannelDefinitionViewModel SelectedItem { get; set; }

        public async Task Add()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Add webhook notification channel";

            var m = new WebhookNotificationChannelDefinition();
            var vm = new WebhookNotificationChannelDefinitionViewModel(m);
            w.DataContext = vm;
            vm.Enabled = true;
            vm.Id = Guid.NewGuid().ToString();

            await ChildWindowManager.ShowChildWindowAsync(this.GetWindow(), w);

            if (w.Result == MessageDialogResult.Affirmative)
            {
                this.model.Add(m);
                this.ViewModels.Add(vm);
            }
        }
        public async Task Edit()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Edit webhook notification channel";

            var m = JsonConvert.DeserializeObject<WebhookNotificationChannelDefinition>(JsonConvert.SerializeObject(this.SelectedItem.Model));
            var vm = new WebhookNotificationChannelDefinitionViewModel(m);

            w.DataContext = vm;

            await ChildWindowManager.ShowChildWindowAsync(this.GetWindow(), w);

            if (w.Result == MessageDialogResult.Affirmative)
            {
                this.model.Remove(this.SelectedItem.Model);
                this.ViewModels.Remove(this.SelectedItem);
                this.model.Add(m);
                this.ViewModels.Add(vm);
                this.SelectedItem = vm;
            }
        }

        public bool CanEdit => this.SelectedItem != null;

        public async Task Delete()
        {
            if (this.SelectedItem == null)
            {
                return;
            }

            MetroDialogSettings s = new MetroDialogSettings
            {
                AnimateShow = false,
                AnimateHide = false
            };

            if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", "Are you sure you want to delete this channel?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
            {
                this.model.Remove(this.SelectedItem.Model);
                this.ViewModels.Remove(this.SelectedItem);
                this.SelectedItem = this.ViewModels.FirstOrDefault();
            }
        }

        public bool CanDelete => this.SelectedItem != null;

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public UIElement View { get; set; }
    }
}
