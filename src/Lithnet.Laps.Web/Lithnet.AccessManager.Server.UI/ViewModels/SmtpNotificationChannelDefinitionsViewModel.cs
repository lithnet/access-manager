using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Newtonsoft.Json;
using NLog.Targets.Wrappers;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SmtpNotificationChannelDefinitionsViewModel : PropertyChangedBase, IHaveDisplayName, IViewAware
    {
        private readonly IList<SmtpNotificationChannelDefinition> model;

        private readonly IDialogCoordinator dialogCoordinator;

        public BindableCollection<SmtpNotificationChannelDefinitionViewModel> ViewModels { get; }

        public SmtpNotificationChannelDefinitionsViewModel(IList<SmtpNotificationChannelDefinition> model, IDialogCoordinator dialogCoordinator)
        {
            this.model = model;
            this.dialogCoordinator = dialogCoordinator;
            this.ViewModels = new BindableCollection<SmtpNotificationChannelDefinitionViewModel>(model.Select(t => new SmtpNotificationChannelDefinitionViewModel(t)));
        }
 
        public string DisplayName { get; set; } = "SMTP";

        public SmtpNotificationChannelDefinitionViewModel SelectedItem { get; set; }

        public async Task Add()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Add SMTP notification channel";
            var m = new SmtpNotificationChannelDefinition();
            var vm = new SmtpNotificationChannelDefinitionViewModel(m);
            w.DataContext = vm;
            vm.Enabled = true;
            vm.Id = Guid.NewGuid().ToString();

            await ChildWindowManager.ShowChildWindowAsync(this.GetWindow(), w);

            if (w.Result == MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative)
            {
                this.model.Add(m);
                this.ViewModels.Add(vm);
            }
        }
        public async Task Edit()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Edit SMTP notification channel";
            var m = JsonConvert.DeserializeObject<SmtpNotificationChannelDefinition>(JsonConvert.SerializeObject(this.SelectedItem.Model));
            var vm = new SmtpNotificationChannelDefinitionViewModel(m);

            w.DataContext = vm;

            await ChildWindowManager.ShowChildWindowAsync(this.GetWindow(), w);

            if (w.Result == MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative)
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
