using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using MahApps.Metro.SimpleChildWindow;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class JitConfigurationViewModel : PropertyChangedBase, IViewAware, IHaveDisplayName
    {
        private readonly JitConfigurationOptions jitOptions;

        private readonly IDirectory directory;

        private readonly IDialogCoordinator dialogCoordinator;

        private readonly IJitGroupMappingViewModelFactory groupMappingFactory;

        public UIElement View { get; set; }

        public string DisplayName { get; set; } = "Just-in-time access";

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.UserClockSolid;
        //<iconPacks:PathIconMaterial Kind="TimerOutline" />
        //<iconPacks:PathIconModern Kind="TimerCheck" />

        public JitConfigurationViewModel(JitConfigurationOptions jitOptions, IDialogCoordinator dialogCoordinator, IDirectory directory, IJitGroupMappingViewModelFactory groupMappingFactory)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.directory = directory;
            this.jitOptions = jitOptions;
            this.groupMappingFactory = groupMappingFactory;

            this.PopulatePamSupportState();

            this.GroupMappings = new BindableCollection<JitGroupMappingViewModel>();

            foreach (var m in this.jitOptions.JitGroupMappings)
            {
                this.GroupMappings.Add(groupMappingFactory.CreateViewModel(m));
            }
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public BindableCollection<JitGroupMappingViewModel> GroupMappings { get; }

        public JitGroupMappingViewModel SelectedGroupMapping { get; set; }

        public string PamSupportState { get; set; } = "unknown";

        public bool EnableJitGroupCreation
        {
            get => this.jitOptions.EnableJitGroupCreation;
            set => this.jitOptions.EnableJitGroupCreation = value;
        }

        private void PopulatePamSupportState()
        {
            //if (this.forest.ForestModeLevel < 2)
            //{
            //    this.PamSupportState = "Just-in-time access is not supported in this forest";
            //    return;
            //}

            //if (this.forest.ForestModeLevel < 7)
            //{
            //    this.PamSupportState =
            //        "Just-in-time access is supported in this forest using legacy dynamic objects. Consider raising the forest functional level to Windows Server 2016 to enable time-based group membership support";
            //    return;
            //}

            //if (this.directory.IsPamFeatureEnabled(this.forest.RootDomain.Name))
            //{
            //    this.PamSupportState =
            //        "Full support for just-in-time access using temporary group membership is available in this forest";
            //}
            //else
            //{
            //    this.PamSupportState =
            //        "Just-in-time access is supported in this forest using legacy dynamic objects. Consider enabling the 'Privileged Access Management' feature in this forest to allow time-based group membership";
            //}
        }

        public async Task Add()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Add mapping";
            w.SaveButtonIsDefault = true;
            var m = new JitGroupMapping() {GroupType = GroupType.DomainLocal, GroupNameTemplate = "JIT-{computerName}"};
            var vm = this.groupMappingFactory.CreateViewModel(m);
            w.DataContext = vm;
            
            await this.GetWindow().ShowChildWindowAsync(w);

            if (w.Result == MessageDialogResult.Affirmative)
            {
                this.jitOptions.JitGroupMappings.Add(m);
                this.GroupMappings.Add(vm);
            }
        }

        public bool CanEdit => this.SelectedGroupMapping != null;

        public async Task Edit()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Edit mapping";
            w.SaveButtonIsDefault = true;

            var m = JsonConvert.DeserializeObject<JitGroupMapping>(JsonConvert.SerializeObject(this.SelectedGroupMapping.Model));
            var vm = this.groupMappingFactory.CreateViewModel(m);

            w.DataContext = vm;

            await this.GetWindow().ShowChildWindowAsync(w);

            if (w.Result == MessageDialogResult.Affirmative)
            {
                this.jitOptions.JitGroupMappings.Remove(this.SelectedGroupMapping.Model);

                int existingPosition = this.GroupMappings.IndexOf(this.SelectedGroupMapping);

                this.GroupMappings.Remove(this.SelectedGroupMapping);
                this.jitOptions.JitGroupMappings.Add(m);
                this.GroupMappings.Insert(Math.Min(existingPosition, this.GroupMappings.Count), vm);
                this.SelectedGroupMapping = vm;
            }
        }

        public bool CanDelete => this.SelectedGroupMapping != null;

        public async Task Delete()
        {
            MetroDialogSettings s = new MetroDialogSettings
            {
                AnimateShow = false,
                AnimateHide = false
            };

            if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", "Are you sure you want to delete this mapping?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
            {
                var deleting = this.SelectedGroupMapping;
                this.jitOptions.JitGroupMappings.Remove(deleting.Model);
                this.GroupMappings.Remove(deleting);
                this.SelectedGroupMapping = this.GroupMappings.FirstOrDefault();
            }
        }
    }
}
