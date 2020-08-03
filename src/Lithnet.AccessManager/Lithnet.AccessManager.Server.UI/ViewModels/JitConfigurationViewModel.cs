using System;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using ControlzEx.Standard;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.PowerShell.Commands;
using Newtonsoft.Json;
using Stylet;
using NativeMethods = Lithnet.AccessManager.Server.UI.Interop.NativeMethods;

namespace Lithnet.AccessManager.Server.UI
{
    public class JitConfigurationViewModel : PropertyChangedBase, IViewAware, IHaveDisplayName
    {
        private readonly JitConfigurationOptions jitOptions;

        private readonly IDirectory directory;

        private readonly IDialogCoordinator dialogCoordinator;

        private readonly IJitGroupMappingViewModelFactory groupMappingFactory;

        private readonly IJitDomainStatusViewModelFactory jitDomainStatusFactory;

        private readonly IServiceSettingsProvider serviceSettings;

        public UIElement View { get; set; }

        public string DisplayName { get; set; } = "Just-in-time access";

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.UserClockSolid;

        public JitConfigurationViewModel(JitConfigurationOptions jitOptions, IDialogCoordinator dialogCoordinator, IDirectory directory, IJitGroupMappingViewModelFactory groupMappingFactory, INotifiableEventPublisher eventPublisher, IJitDomainStatusViewModelFactory jitDomainStatusFactory, IServiceSettingsProvider serviceSettings)
        {
            this.dialogCoordinator = dialogCoordinator;
            this.directory = directory;
            this.jitOptions = jitOptions;
            this.groupMappingFactory = groupMappingFactory;
            this.jitDomainStatusFactory = jitDomainStatusFactory;
            this.serviceSettings = serviceSettings;

            this.GroupMappings = new BindableCollection<JitGroupMappingViewModel>();

            foreach (var m in this.jitOptions.JitGroupMappings)
            {
                this.GroupMappings.Add(groupMappingFactory.CreateViewModel(m));
            }

            this.Domains = new BindableCollection<JitDomainStatusViewModel>();

            this.BuildDomainList();

            eventPublisher.Register(this);
        }

        private void BuildDomainList()
        {
            JitDomainStatusViewModel vm;

            foreach (var d in Forest.GetCurrentForest().Domains.OfType<Domain>())
            {
                vm = jitDomainStatusFactory.CreateViewModel(d, this.GetDynamicGroupMapping(d));
                this.Domains.Add(vm);
            }

            foreach (var trust in Domain.GetCurrentDomain().Forest.GetAllTrustRelationships().OfType<TrustRelationshipInformation>())
            {
                if (trust.TrustDirection == TrustDirection.Inbound || trust.TrustDirection == TrustDirection.Bidirectional)
                {
                    var forest = Forest.GetForest(new DirectoryContext(DirectoryContextType.Forest, trust.TargetName));

                    foreach (var d in forest.Domains.OfType<Domain>())
                    {
                        vm = jitDomainStatusFactory.CreateViewModel(d, this.GetDynamicGroupMapping(d));
                        this.Domains.Add(vm);
                    }
                }
            }
        }

        private JitDynamicGroupMapping GetDynamicGroupMapping(Domain domain)
        {
            SecurityIdentifier sid = domain.GetDirectoryEntry().GetPropertySid("objectSid");

            foreach (var item in this.jitOptions.DynamicGroupMappings)
            {
                if (string.Equals(item.Domain, sid.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            return null;
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        [NotifiableCollection]
        public BindableCollection<JitGroupMappingViewModel> GroupMappings { get; }

        public BindableCollection<JitDomainStatusViewModel> Domains { get; }

        public JitDomainStatusViewModel SelectedDomain { get; set; }

        public JitGroupMappingViewModel SelectedGroupMapping { get; set; }

        [NotifiableProperty]
        public bool EnableJitGroupCreation
        {
            get => this.jitOptions.EnableJitGroupCreation;
            set => this.jitOptions.EnableJitGroupCreation = value;
        }

        public async Task Add()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Add mapping";
            w.SaveButtonIsDefault = true;
            var m = new JitGroupMapping() { GroupType = GroupType.DomainLocal, GroupNameTemplate = "JIT-{computerName}" };
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

        public bool CanSelectDynamicGroupOU => this.SelectedDomain != null && !this.SelectedDomain.IsPamEnabled;

        public void SelectDynamicGroupOU()
        {
            JitDomainStatusViewModel current = this.SelectedDomain;
            string oldOU = current.DynamicGroupOU;

            var container =
                NativeMethods.ShowContainerDialog(this.GetHandle(), "Select container", "Select container", $"LDAP://{current.Domain.Name}", $"LDAP://{current.Domain.Name}/{current.DynamicGroupOU}");

            if (container != null)
            {
                if (current.Mapping == null)
                {
                    current.Mapping = new JitDynamicGroupMapping();
                    current.Mapping.Domain = current.Domain.GetDirectoryEntry().GetPropertySid("objectSid").ToString();
                    this.jitOptions.DynamicGroupMappings.Add(current.Mapping);
                }

                current.Mapping.GroupOU = container;
                current.DynamicGroupOU = container;

                if (oldOU != container)
                {
                    this.HasBeenChanged++;
                }
            }
        }

        [NotifiableProperty]
        public int HasBeenChanged { get; set; }

        public bool CanDelegateJitGroupPermission => !string.IsNullOrWhiteSpace(this.SelectedGroupMapping?.GroupOU);

        public void DelegateJitGroupPermission()
        {
            var current = this.SelectedGroupMapping;

            var vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Run this script to grant access for the AMS service account to manage groups in this OU",
                ScriptText = ScriptTemplates.GrantGroupPermissions
                    .Replace("{serviceAccount}", this.serviceSettings.GetServiceAccount().ToString(), StringComparison.OrdinalIgnoreCase)
                    .Replace("{ou}", current.GroupOU)
                    .Replace("{domain}", directory.GetDomainNameDnsFromDn(current.GroupOU))
            };

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();
        }

        public bool CanDelegateDynamicGroupPermission => !string.IsNullOrWhiteSpace(this.SelectedDomain?.DynamicGroupOU) && !this.SelectedDomain.IsPamEnabled;


        public void DelegateDynamicGroupPermission()
        {
            var current = this.SelectedDomain;

            var vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Run this script to grant access for the AMS service account to manage groups in this OU",
                ScriptText = ScriptTemplates.GrantGroupPermissions
                    .Replace("{serviceAccount}", this.serviceSettings.GetServiceAccount().ToString(), StringComparison.OrdinalIgnoreCase)
                    .Replace("{ou}", current.Mapping.GroupOU)
                    .Replace("{domain}", current.Domain.Name)
            };

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();
        }

        public bool CanEnablePam => this.SelectedDomain != null && this.SelectedDomain.IsPamSupported;

        public async Task EnablePam()
        {
            var current = this.SelectedDomain;

            var vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Run this script to enable the PAM feature in your forest",
                ScriptText = ScriptTemplates.EnablePamFeature
                    .Replace("{domain}", current.Domain.Forest.Name)
            };

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();
            await current.RefreshStatus();
        }

        public async Task RefreshJitStatus()
        {
            foreach (var vm in this.Domains)
            {
                await vm.RefreshStatus();
            }
        }
    }
}
