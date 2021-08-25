using System;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryJitConfigurationViewModel : Screen, IHelpLink
    {
        private readonly JitConfigurationOptions jitOptions;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IViewModelFactory<JitGroupMappingViewModel, JitGroupMapping> groupMappingFactory;
        private readonly IViewModelFactory<JitDomainStatusViewModel, Domain, JitDynamicGroupMapping> jitDomainStatusFactory;
        private readonly IWindowsServiceProvider windowsServiceProvider;
        private readonly INotifyModelChangedEventPublisher eventPublisher;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IDomainTrustProvider domainTrustProvider;
        private readonly IDiscoveryServices discoveryServices;
        private readonly IObjectSelectionProvider objectSelectionProvider;
        private readonly IScriptTemplateProvider scriptTemplateProvider;
        private readonly ILogger<ActiveDirectoryJitConfigurationViewModel> logger;
        private readonly IWindowManager windowManager;
        private readonly IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory;

        public ActiveDirectoryJitConfigurationViewModel(JitConfigurationOptions jitOptions, IDialogCoordinator dialogCoordinator, IViewModelFactory<JitGroupMappingViewModel, JitGroupMapping> groupMappingFactory, INotifyModelChangedEventPublisher eventPublisher, IViewModelFactory<JitDomainStatusViewModel, Domain, JitDynamicGroupMapping> jitDomainStatusFactory, IWindowsServiceProvider windowsServiceProvider, IShellExecuteProvider shellExecuteProvider, IDomainTrustProvider domainTrustProvider, IDiscoveryServices discoveryServices, IObjectSelectionProvider objectSelectionProvider, IScriptTemplateProvider scriptTemplateProvider, ILogger<ActiveDirectoryJitConfigurationViewModel> logger, IWindowManager windowManager, IViewModelFactory<ExternalDialogWindowViewModel, Screen> externalDialogWindowFactory)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.dialogCoordinator = dialogCoordinator;
            this.jitOptions = jitOptions;
            this.groupMappingFactory = groupMappingFactory;
            this.jitDomainStatusFactory = jitDomainStatusFactory;
            this.windowsServiceProvider = windowsServiceProvider;
            this.eventPublisher = eventPublisher;
            this.domainTrustProvider = domainTrustProvider;
            this.discoveryServices = discoveryServices;
            this.objectSelectionProvider = objectSelectionProvider;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.logger = logger;
            this.windowManager = windowManager;
            this.externalDialogWindowFactory = externalDialogWindowFactory;

            this.DisplayName = "Just-in-time access";
            this.GroupMappings = new BindableCollection<JitGroupMappingViewModel>();
            this.Domains = new BindableCollection<JitDomainStatusViewModel>();

        }
        public string ErrorMessageText { get; set; }

        public string ErrorMessageHeaderText { get; set; }

        protected override void OnInitialActivate()
        {
            Task.Run(() => this.Initialize());
        }

        private void Initialize()
        {
            try
            {
                foreach (var m in this.jitOptions.JitGroupMappings)
                {
                    this.GroupMappings.Add(groupMappingFactory.CreateViewModel(m));
                }

                this.BuildDomainList();

                this.eventPublisher.Register(this);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "The view model failed to initialize");
                this.ErrorMessageText = ex.ToString();
                this.ErrorMessageHeaderText = "Initialization error";
            }
        }

        private void BuildDomainList()
        {
            foreach (Domain d in this.domainTrustProvider.GetDomains())
            {
                JitDomainStatusViewModel vm = jitDomainStatusFactory.CreateViewModel(d, this.GetDynamicGroupMapping(d));
                this.Domains.Add(vm);
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

        [NotifyModelChangedCollection]
        public BindableCollection<JitGroupMappingViewModel> GroupMappings { get; }

        public BindableCollection<JitDomainStatusViewModel> Domains { get; }

        public JitDomainStatusViewModel SelectedDomain { get; set; }

        public JitGroupMappingViewModel SelectedGroupMapping { get; set; }

        [NotifyModelChangedProperty]
        public bool EnableJitGroupCreation
        {
            get => this.jitOptions.EnableJitGroupCreation;
            set => this.jitOptions.EnableJitGroupCreation = value;
        }

        public async Task Add()
        {
            try
            {
                DialogWindow w = new DialogWindow();
                w.Title = "Add mapping";
                w.SaveButtonIsDefault = true;
                var m = new JitGroupMapping() { GroupType = GroupType.DomainLocal, GroupNameTemplate = "JIT-%computerName%" };
                var vm = this.groupMappingFactory.CreateViewModel(m);
                w.DataContext = vm;

                await this.GetWindow().ShowChildWindowAsync(w);

                if (w.Result == MessageDialogResult.Affirmative)
                {
                    this.jitOptions.JitGroupMappings.Add(m);
                    this.GroupMappings.Add(vm);

                    this.SelectedGroupMapping = vm;

                    if (await this.dialogCoordinator.ShowMessageAsync(this, "Delegate access", "Would you like to show the script to delegate AMS permissions for this OU now?", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                    {
                        await this.DelegateJitGroupPermission();
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanEdit => this.SelectedGroupMapping != null;

        public async Task Edit()
        {
            try
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
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanDelete => this.SelectedGroupMapping != null;

        public async Task Delete()
        {
            try
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
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanSelectDynamicGroupOU => this.SelectedDomain != null && !this.SelectedDomain.IsPamEnabled;

        public async Task SelectDynamicGroupOU()
        {
            try
            {
                JitDomainStatusViewModel current = this.SelectedDomain;
                string oldOU = current.DynamicGroupOU;

                string basePath = this.discoveryServices.GetFullyQualifiedRootAdsPath(current.DynamicGroupOU);
                string initialPath = this.discoveryServices.GetFullyQualifiedAdsPath(current.DynamicGroupOU);

                if (!this.objectSelectionProvider.SelectContainer(this, "Select container", "Select container", basePath, initialPath, out string container))
                {
                    return;
                }

                if (!string.Equals(current.Domain.Name, this.discoveryServices.GetDomainNameDns(container)))
                {
                    await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"The dynamic group OU must be in the {current.Domain.Name} domain");
                    return;
                }

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

                this.NotifyOfPropertyChange(nameof(CanDelegateJitGroupPermission));
                this.NotifyOfPropertyChange(nameof(CanDelegateDynamicGroupPermission));
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        [NotifyModelChangedProperty]
        public int HasBeenChanged { get; set; }

        public bool CanDelegateJitGroupPermission => !string.IsNullOrWhiteSpace(this.SelectedGroupMapping?.GroupOU);

        public async Task DelegateJitGroupPermission()
        {
            try
            {
                var current = this.SelectedGroupMapping;

                var vm = new ScriptContentViewModel(this.dialogCoordinator)
                {
                    HelpText = "Run this script to grant access for the AMS service account to manage groups in this OU",
                    ScriptText = this.scriptTemplateProvider.GrantGroupPermissions
                        .Replace("{serviceAccount}", this.windowsServiceProvider.GetServiceAccountSid().ToString(), StringComparison.OrdinalIgnoreCase)
                        .Replace("{ou}", current.GroupOU)
                        .Replace("{domain}", discoveryServices.GetDomainNameDns(current.GroupOU))
                };

                var evm = this.externalDialogWindowFactory.CreateViewModel(vm);
                windowManager.ShowDialog(evm);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanDelegateDynamicGroupPermission => !string.IsNullOrWhiteSpace(this.SelectedDomain?.DynamicGroupOU) && !this.SelectedDomain.IsPamEnabled;

        public async Task DelegateDynamicGroupPermission()
        {
            try
            {
                var current = this.SelectedDomain;

                var vm = new ScriptContentViewModel(this.dialogCoordinator)
                {
                    HelpText = "Run this script to grant access for the AMS service account to manage groups in this OU",
                    ScriptText = this.scriptTemplateProvider.GrantGroupPermissions
                        .Replace("{serviceAccount}", this.windowsServiceProvider.GetServiceAccountSid().ToString(), StringComparison.OrdinalIgnoreCase)
                        .Replace("{ou}", current.Mapping.GroupOU)
                        .Replace("{domain}", current.Domain.Name)
                };

                var evm = this.externalDialogWindowFactory.CreateViewModel(vm);
                windowManager.ShowDialog(evm);
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanEnablePam => this.SelectedDomain != null && this.SelectedDomain.IsPamSupported;

        public async Task EnablePam()
        {
            try
            {
                var current = this.SelectedDomain;

                var vm = new ScriptContentViewModel(this.dialogCoordinator)
                {
                    HelpText = "Run this script to enable the PAM feature in your forest",
                    ScriptText = this.scriptTemplateProvider.EnablePamFeature
                        .Replace("{domain}", current.Domain.Forest.Name)
                };

                var evm = this.externalDialogWindowFactory.CreateViewModel(vm);
                windowManager.ShowDialog(evm);
                await current.RefreshStatus();
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public async Task RefreshJitStatus()
        {
            try
            {
                foreach (var vm in this.Domains)
                {
                    await vm.RefreshStatus();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public string HelpLink => Constants.HelpLinkPageJitAccess;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
