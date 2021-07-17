using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Stylet;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Server.Providers;
using Microsoft.Graph;
using Domain = System.DirectoryServices.ActiveDirectory.Domain;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class SecurityDescriptorTargetViewModel : ValidatingModelBase, IViewAware
    {
        private readonly IActiveDirectory directory;
        private readonly ILogger<SecurityDescriptorTargetViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IViewModelFactory<NotificationChannelSelectionViewModel, AuditNotificationChannels> notificationChannelFactory;
        private readonly IDiscoveryServices discoveryServices;
        private readonly ILocalSam localSam;
        private readonly SecurityDescriptorTargetViewModelDisplaySettings displaySettings;
        private readonly IObjectSelectionProvider objectSelectionProvider;
        private readonly ScriptTemplateProvider scriptTemplateProvider;
        private readonly IAmsLicenseManager licenseManager;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IViewModelFactory<SelectTargetTypeViewModel> targetTypeFactory;
        private readonly IViewModelFactory<AzureAdObjectSelectorViewModel> aadSelectorFactory;
        private readonly IAadGraphApiProvider graphProvider;
        private readonly IDeviceProvider deviceProvider;
        private readonly IViewModelFactory<AmsGroupSelectorViewModel> amsGroupSelectorFactory;
        private readonly IViewModelFactory<AmsDeviceSelectorViewModel> amsDeviceSelectorFactory;
        private readonly IAmsGroupProvider amsGroupProvider;

        private string jitGroupDisplayName;

        public Task Initialization { get; }

        public SecurityDescriptorTarget Model { get; }

        public SecurityDescriptorTargetViewModel(SecurityDescriptorTarget model, SecurityDescriptorTargetViewModelDisplaySettings displaySettings, IViewModelFactory<NotificationChannelSelectionViewModel, AuditNotificationChannels> notificationChannelFactory, IFileSelectionViewModelFactory fileSelectionViewModelFactory, IAppPathProvider appPathProvider, ILogger<SecurityDescriptorTargetViewModel> logger, IDialogCoordinator dialogCoordinator, IModelValidator<SecurityDescriptorTargetViewModel> validator, IActiveDirectory directory, IDiscoveryServices discoveryServices, ILocalSam localSam, IObjectSelectionProvider objectSelectionProvider, ScriptTemplateProvider scriptTemplateProvider, IAmsLicenseManager licenseManager, IShellExecuteProvider shellExecuteProvider, IViewModelFactory<SelectTargetTypeViewModel> targetTypeFactory, IViewModelFactory<AzureAdObjectSelectorViewModel> aadSelectorFactory, IAadGraphApiProvider graphProvider, IDeviceProvider deviceProvider, IViewModelFactory<AmsGroupSelectorViewModel> amsGroupSelectorFactory, IViewModelFactory<AmsDeviceSelectorViewModel> amsDeviceSelectorFactory, IAmsGroupProvider amsGroupProvider)
        {
            this.directory = directory;
            this.Model = model;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.notificationChannelFactory = notificationChannelFactory;
            this.Validator = validator;
            this.discoveryServices = discoveryServices;
            this.localSam = localSam;
            this.displaySettings = displaySettings ?? new SecurityDescriptorTargetViewModelDisplaySettings();
            this.objectSelectionProvider = objectSelectionProvider;
            this.scriptTemplateProvider = scriptTemplateProvider;
            this.licenseManager = licenseManager;
            this.shellExecuteProvider = shellExecuteProvider;
            this.targetTypeFactory = targetTypeFactory;
            this.aadSelectorFactory = aadSelectorFactory;
            this.graphProvider = graphProvider;
            this.deviceProvider = deviceProvider;
            this.amsGroupSelectorFactory = amsGroupSelectorFactory;
            this.amsDeviceSelectorFactory = amsDeviceSelectorFactory;
            this.amsGroupProvider = amsGroupProvider;

            this.Script = fileSelectionViewModelFactory.CreateViewModel(model, () => model.Script, appPathProvider.ScriptsPath);
            this.Script.DefaultFileExtension = "ps1";
            this.Script.Filter = "PowerShell script|*.ps1";
            this.Script.NewFileContent = this.scriptTemplateProvider.GetAuthorizationResponse;
            this.Script.ShouldValidate = false;
            this.Script.PropertyChanged += Script_PropertyChanged;
            this.Initialization = this.Initialize();
        }

        private async Task Initialize()
        {
            this.Notifications = notificationChannelFactory.CreateViewModel(this.Model.Notifications);
            this.DisplayName = this.CachedTargetName ?? this.Target;
            this.jitGroupDisplayName = this.JitAuthorizingGroup;
            await this.UpdateDisplayName(false);
            await this.UpdateJitGroupDisplayName();
            await this.ValidateAsync();
            this.LastModifiedBy = await this.GetNameFromActiveDirectorySidOrDefaultAsync(this.Model.LastModifiedBy) ?? this.Model.LastModifiedBy;
            this.CreatedBy = await this.GetNameFromActiveDirectorySidOrDefaultAsync(this.Model.CreatedBy) ?? this.Model.CreatedBy;
        }

        private void Script_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.Validate();
        }

        public NotificationChannelSelectionViewModel Notifications { get; private set; }

        public AuthorizationMode AuthorizationMode
        {
            get => this.Model.AuthorizationMode; set
            {
                this.Model.AuthorizationMode = value;
                this.Script.ShouldValidate = (value == AuthorizationMode.PowershellScript);
            }
        }

        [DependsOn(nameof(Expiry), nameof(ExpireRule), nameof(IsDisabled))]
        public string Status
        {
            get
            {
                return this.IsDisabled ? "Disabled" : this.HasExpired ? "Expired" : "Active";
            }
        }

        public bool IsEditing
        {
            get => !this.IsAdding;
            set => this.IsAdding = !value;
        }

        public bool IsAdding { get; set; } = true;


        public bool IsDisabled
        {
            get => this.Model.Disabled;
            set => this.Model.Disabled = value;
        }

        public DateTime? Expiry
        {
            get => this.Model.Expiry?.ToLocalTime();
            set => this.Model.Expiry = value?.ToUniversalTime();
        }

        public bool ExpireRule
        {
            get => this.Expiry != null;
            set => this.Expiry = value ? this.Expiry == null ? DateTime.Now.AddDays(30) : this.Expiry : null;
        }

        public bool HasExpired
        {
            get => this.Model.HasExpired();
        }

        public string Notes
        {
            get => this.Model.Notes;
            set => this.Model.Notes = value;
        }

        public string TargetObjectId { get => this.Model.TargetObjectId; set => this.Model.TargetObjectId = value; }

        public string TargetAuthorityId { get => this.Model.TargetAuthorityId; set => this.Model.TargetAuthorityId = value; }

        public string CreatedBy { get; private set; }

        public string LastModifiedBy { get; private set; }

        public DateTime? Created => this.Model.Created?.ToLocalTime();

        public DateTime? LastModified => this.Model.LastModified?.ToLocalTime();

        public bool IsModePermission { get => this.AuthorizationMode == AuthorizationMode.SecurityDescriptor; set => this.AuthorizationMode = value ? AuthorizationMode.SecurityDescriptor : AuthorizationMode.PowershellScript; }

        public bool IsModeScript { get => this.AuthorizationMode == AuthorizationMode.PowershellScript; set => this.AuthorizationMode = value ? AuthorizationMode.PowershellScript : AuthorizationMode.SecurityDescriptor; }

        public string Target
        {
            get => this.Model.Target;
            set
            {
                this.Model.Target = value;
                _ = this.UpdateDisplayName(true);
            }
        }

        public async Task UpdateDisplayName(bool force)
        {
            if (this.Target == null)
            {
                this.DisplayName = null;
                this.CachedTargetName = null;
            }
            else if (this.Type == TargetType.AdContainer)
            {
                this.DisplayName = this.Target;
            }
            else
            {
                if (force || this.CachedTargetName == null)
                {
                    var resolvedName = await this.GetTargetNameFromSidOrDefaultAsync(this.Target);

                    if (resolvedName == null)
                    {
                        this.DisplayName = this.Target;
                        this.CachedTargetName = null;
                    }
                    else
                    {
                        this.CachedTargetName = resolvedName;
                        this.DisplayName = resolvedName;
                    }
                }
                else
                {
                    this.DisplayName = this.CachedTargetName;
                }
            }
        }

        private async Task UpdateJitGroupDisplayName()
        {
            this.jitGroupDisplayName = await this.GetNameFromActiveDirectorySidOrDefaultAsync(this.JitAuthorizingGroup) ?? this.JitAuthorizingGroup;
            this.NotifyOfPropertyChange(nameof(JitGroupDisplayName));
        }

        public FileSelectionViewModel Script { get; }

        public string Id => this.Model.Id;

        public TargetType Type { get => this.Model.Type; set => this.Model.Type = value; }

        public string SecurityDescriptor { get => this.Model.SecurityDescriptor; set => this.Model.SecurityDescriptor = value; }

        public string Description { get => this.Model.Description; set => this.Model.Description = value; }

        public string JitAuthorizingGroup { get => this.Model.Jit.AuthorizingGroup; set => this.Model.Jit.AuthorizingGroup = value; }

        public bool IsScriptVisible => this.displaySettings.IsScriptVisible;

        public string JitGroupDisplayName
        {
            get => jitGroupDisplayName;
            set
            {
                if (value.Contains("{computerName}", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("%computerName%", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("{computerDomain}", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("%computerDomain%", StringComparison.OrdinalIgnoreCase))
                {
                    this.JitAuthorizingGroup = value;
                }
                else
                {
                    if (this.directory.TryGetGroup(value, out IActiveDirectoryGroup group))
                    {
                        this.JitAuthorizingGroup = group.Sid.ToString();
                    }
                    else
                    {
                        this.JitAuthorizingGroup = value;
                    }
                }

                _ = this.UpdateJitGroupDisplayName();
            }
        }

        public TimeSpan JitExpireAfter { get => this.Model.Jit.ExpireAfter; set => this.Model.Jit.ExpireAfter = value; }

        public TimeSpan LapsExpireAfter { get => this.Model.Laps.ExpireAfter; set => this.Model.Laps.ExpireAfter = value; }

        public int LapsExpireMinutes { get => (int)this.LapsExpireAfter.TotalMinutes; set => this.LapsExpireAfter = new TimeSpan(0, Math.Max(value, 15), 0); }

        public bool ExpireLapsPassword
        {
            get => this.LapsExpireAfter.TotalSeconds > 0;
            set
            {
                if (value)
                {
                    if (this.LapsExpireAfter.TotalSeconds <= 0)
                    {
                        this.LapsExpireAfter = new TimeSpan(0, 15, 0);
                    }
                }
                else
                {
                    this.LapsExpireAfter = new TimeSpan(0);
                }
            }
        }

        public int JitExpireMinutes { get => (int)this.JitExpireAfter.TotalMinutes; set => this.JitExpireAfter = new TimeSpan(0, Math.Max(value, 15), 0); }

        public PasswordStorageLocation RetrievalLocation { get => this.Model.Laps.RetrievalLocation; set => this.Model.Laps.RetrievalLocation = value; }

        public string DisplayName { get; private set; }

        public string CachedTargetName { get => this.Model.CachedTargetName; set => this.Model.CachedTargetName = value; }

        public bool ShowLapsOptions => this.IsModeScript || SdHasMask(this.SecurityDescriptor, AccessMask.LocalAdminPassword);

        public bool ShowJitOptions => this.IsModeScript || (this.Type.IsAdTarget() && SdHasMask(this.SecurityDescriptor, AccessMask.Jit));

        public bool CanEdit => this.Target != null;

        public bool IsScriptPermissionAllowed => this.licenseManager.IsFeatureEnabled(LicensedFeatures.PowerShellAcl) && this.Target != null;

        public bool IsScriptPermissionNotAllowed => !this.IsScriptPermissionAllowed;

        public bool ShowPowerShellEnterpriseEditionBadge => this.IsScriptVisible && !this.licenseManager.IsFeatureCoveredByFullLicense(LicensedFeatures.PowerShellAcl);

        public bool ShowLapsHistoryEnterpriseEditionBadge => !this.licenseManager.IsFeatureCoveredByFullLicense(LicensedFeatures.PowerShellAcl) && SdHasMask(this.SecurityDescriptor, AccessMask.LocalAdminPasswordHistory);

        public bool ShowTargetTypeEnterpriseEditionBadge => this.Type.IsAadTarget() || this.Type.IsAmsTarget();

        public bool CanEditPermissions => this.CanEdit && this.AuthorizationMode == AuthorizationMode.SecurityDescriptor && this.Target != null;

        public bool CanSelectScript => this.CanEdit && this.AuthorizationMode == AuthorizationMode.PowershellScript;

        public async Task EditPermissions()
        {
            await this.EditPermissionsInternal(this.SecurityDescriptor);
        }

        private async Task EditPermissionsInternal(string startingSd)
        {
            try
            {
                List<SiAccess> rights = new List<SiAccess>
                {
                    new SiAccess((uint)AccessMask.LocalAdminPassword, "Local admin password", InheritFlags.SiAccessGeneral),
                    new SiAccess((uint)AccessMask.LocalAdminPasswordHistory, "Local admin password history", InheritFlags.SiAccessGeneral),
                };

                if (this.Type.IsAdTarget())
                {
                    rights.Add(new SiAccess((uint)AccessMask.Jit, "Just-in-time access", InheritFlags.SiAccessGeneral));
                    rights.Add(new SiAccess((uint)AccessMask.BitLocker, "BitLocker recovery passwords", InheritFlags.SiAccessGeneral));
                }

                GenericSecurityDescriptor sd = this.GetSecurityDescriptorForEditing(startingSd);

                string targetServer = this.GetDcForTargetOrDefault();

                SiObjectInfoFlags flags = SiObjectInfoFlags.EditPermissions;

                GenericMapping mapping = new GenericMapping
                {
                    GenericAll = (uint)(AccessMask.LocalAdminPassword | AccessMask.LocalAdminPasswordHistory | AccessMask.Jit | AccessMask.BitLocker)
                };

                BasicSecurityInformation info = new BasicSecurityInformation(
                    flags,
                    this.DisplayName,
                    rights,
                    sd,
                    mapping,
                    targetServer
                );

                if (NativeMethods.EditSecurity(this.GetHandle(), info))
                {
                    info.SecurityDescriptor.Owner = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                    info.SecurityDescriptor.Group = null;
                    this.SecurityDescriptor = info.SecurityDescriptor.GetSddlForm(AccessControlSections.All);
                }

                await this.ValidateAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Edit security error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the object security\r\n{ex.Message}");
            }
        }

        private GenericSecurityDescriptor GetSecurityDescriptorForEditing(string sd)
        {
            var rawSd = new RawSecurityDescriptor(sd);
            if (this.Type.IsAdTarget())
            {
                return rawSd;
            }

            var csd = new CommonSecurityDescriptor(false, false, rawSd);

            foreach (var ace in csd.DiscretionaryAcl.OfType<CommonAce>())
            {
                csd.DiscretionaryAcl.RemoveAccess((AccessControlType)ace.AceType, ace.SecurityIdentifier, (int)AccessMask.Jit, InheritanceFlags.None, PropagationFlags.None);
                csd.DiscretionaryAcl.RemoveAccess((AccessControlType)ace.AceType, ace.SecurityIdentifier, (int)AccessMask.BitLocker, InheritanceFlags.None, PropagationFlags.None);
            }

            return csd;
        }

        public bool CanSelectJitGroup => this.CanEdit;

        private string GetDcForTargetOrDefault()
        {
            if (this.Target == null)
            {
                return this.discoveryServices.GetDomainNameDns();
            }

            string domain = null;

            if (!this.Type.IsAdTarget())
            {
                return this.discoveryServices.GetDomainController(this.discoveryServices.GetDomainNameDns());
            }

            try
            {
                if (this.Type == TargetType.AdContainer)
                {
                    domain = this.discoveryServices.GetDomainNameDns(this.Target);
                }
                else if (this.Target.TryParseAsSid(out SecurityIdentifier sid))
                {
                    domain = this.discoveryServices.GetDomainNameDns(sid);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(EventIDs.UIGenericWarning, ex, "Error getting dc for target");
            }

            return this.discoveryServices.GetDomainController(domain ?? this.discoveryServices.GetDomainNameDns());
        }

        private string GetForestForTargetOrDefault()
        {
            if (string.IsNullOrWhiteSpace(this.Target))
            {
                return this.discoveryServices.GetForestNameDns();
            }

            if (!this.Type.IsAdTarget())
            {
                return this.discoveryServices.GetForestNameDns();
            }

            string forest = null;
            try
            {
                if (this.Type == TargetType.AdContainer)
                {
                    forest = discoveryServices.GetForestNameDns(this.Target);
                }
                else if (this.Target.TryParseAsSid(out SecurityIdentifier sid))
                {
                    forest = Domain.GetDomain(new DirectoryContext(DirectoryContextType.Domain, this.discoveryServices.GetDomainNameDns(sid))).Forest.Name;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(EventIDs.UIGenericWarning, ex, "Error resolving forest name");
            }

            forest ??= this.discoveryServices.GetForestNameDns();

            return forest;
        }


        public async Task SelectJitGroup()
        {
            try
            {
                if (this.objectSelectionProvider.GetGroup(this, this.GetDcForTargetOrDefault(), out SecurityIdentifier sid))
                {
                    this.JitAuthorizingGroup = sid.ToString();
                    await this.UpdateJitGroupDisplayName();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Select JIT group error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
            }
        }

        public bool CanImportJitAdminsFromComputer => this.CanEdit && this.AuthorizationMode == AuthorizationMode.SecurityDescriptor && this.Target != null;

        public async Task ImportJitAdminsFromComputer()
        {
            try
            {
                if (!this.objectSelectionProvider.GetComputer(this, this.GetForestForTargetOrDefault(), out SecurityIdentifier sid))
                {
                    return;
                }

                if (!this.directory.TryGetComputer(sid, out IActiveDirectoryComputer computer))
                {
                    await this.dialogCoordinator.ShowMessageAsync(this, "Error", "Unable to locate computer in the directory");
                    return;
                }

                SecurityIdentifier localMachineSid = null;

                try
                {
                    localMachineSid = localSam.GetLocalMachineAuthoritySid(computer.DnsHostName);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(EventIDs.UIGenericWarning, ex, "Unable to connect to get SID from remote computer {computer}", computer.DnsHostName);
                }

                IList<SecurityIdentifier> members;

                try
                {
                    members = this.localSam.GetLocalGroupMembers(computer.DnsHostName, this.localSam.GetBuiltInAdministratorsGroupNameOrDefault(computer.DnsHostName));
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.UIGenericError, ex, "Unable to connect to remote computer {computer}", computer.DnsHostName);
                    await this.dialogCoordinator.ShowMessageAsync(this, "Error", "Unable to connect to the computer");
                    return;
                }

                this.SecurityDescriptor ??= "O:SYD:";
                CommonSecurityDescriptor csd = new CommonSecurityDescriptor(false, false, this.SecurityDescriptor);


                foreach (var member in members)
                {
                    if (localMachineSid != null)
                    {
                        if (member.IsEqualDomainSid(localMachineSid))
                        {
                            continue;
                        }
                    }

                    try
                    {
                        if (!directory.TryGetPrincipal(member, out _))
                        {
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogTrace(ex, "Unable to find principal {principal} in the directory", member);
                    }

                    csd.DiscretionaryAcl.AddAccess(AccessControlType.Allow, member, (int)AccessMask.Jit, InheritanceFlags.None, PropagationFlags.None);
                }

                await this.EditPermissionsInternal(csd.GetSddlForm(AccessControlSections.All));
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Unable to import users from computer");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", "Unable to import users from computer");
            }
        }

        public async Task SelectTarget()
        {
            try
            {
                SelectTargetTypeViewModel vm = this.targetTypeFactory.CreateViewModel();
                vm.TargetType = this.Type;

                DialogWindow w = new DialogWindow
                {
                    Title = "Select target type",
                    DataContext = vm,
                    SaveButtonName = "Next...",
                    SaveButtonIsDefault = true
                };

                vm.SelectedForest = this.GetForestForTargetOrDefault();

                await this.GetWindow().ShowChildWindowAsync(w);

                if (w.Result != MessageDialogResult.Affirmative)
                {
                    return;
                }

                if (vm.TargetType == TargetType.AdContainer)
                {
                    ShowContainerDialog(vm.TargetType);
                }
                else if (vm.TargetType.IsAdTarget())
                {
                    this.ShowAdObjectSelector(vm.TargetType, vm.SelectedForest);
                }
                else if (vm.TargetType.IsAadTarget())
                {
                    this.ShowAadObjectSelector(vm.TargetType, vm.SelectedAad.TenantId);
                }
                else if (vm.TargetType.IsAmsTarget())
                {
                    await this.ShowAmsObjectSelector(vm.TargetType);
                }
                else
                {
                    await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Not yet implemented");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Select target error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
            }
        }

        private async Task ShowAmsObjectSelector(TargetType type)
        {
            if (type == TargetType.AmsComputer)
            {
                this.ShowAmsDeviceSelector();
            }
            else if (type == TargetType.AmsGroup)
            {
                this.ShowAmsGroupSelector();
            }
            else
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Not yet implemented");
            }

        }

        private void ShowAmsGroupSelector()
        {
            var selectorVm = this.amsGroupSelectorFactory.CreateViewModel();

            ExternalDialogWindow w = new ExternalDialogWindow()
            {
                Title = "Select AMS group",
                DataContext = selectorVm,
                SaveButtonName = "Select...",
                SaveButtonIsDefault = true,
                Owner = this.GetWindow()
            };

            if (!w.ShowDialog() ?? false)
            {
                return;
            }

            if (selectorVm.SelectedItem != null)
            {
                this.Target = selectorVm.SelectedItem.Sid;
                this.Type = TargetType.AmsGroup;
                this.CachedTargetName = selectorVm.SelectedItem.Name;
                this.DisplayName = selectorVm.SelectedItem.Name;
                this.TargetObjectId = null;
                this.TargetAuthorityId = null;
            }
        }

        private void ShowAmsDeviceSelector()
        {
            var selectorVm = this.amsDeviceSelectorFactory.CreateViewModel();

            ExternalDialogWindow w = new ExternalDialogWindow()
            {
                Title = "Select AMS device",
                DataContext = selectorVm,
                SaveButtonName = "Select...",
                SaveButtonIsDefault = true,
                Owner = this.GetWindow()
            };

            if (!w.ShowDialog() ?? false)
            {
                return;
            }

            if (selectorVm.SelectedItem != null)
            {
                this.Target = selectorVm.SelectedItem.Sid;
                this.Type = TargetType.AmsComputer;
                this.CachedTargetName = selectorVm.SelectedItem.Name;
                this.DisplayName = selectorVm.SelectedItem.Name;
                this.TargetObjectId = selectorVm.SelectedItem.AuthorityDeviceId;
                this.TargetAuthorityId = selectorVm.SelectedItem.AuthorityId;
            }
        }

        private void ShowAadObjectSelector(TargetType type, string selectedTenant)
        {
            var selectorVm = this.aadSelectorFactory.CreateViewModel();
            selectorVm.Type = type;
            selectorVm.TenantId = selectedTenant;

            ExternalDialogWindow w = new ExternalDialogWindow()
            {
                Title = "Select Azure AD " + (type == TargetType.AadComputer ? "managed computer" : "group"),
                DataContext = selectorVm,
                SaveButtonName = "Select...",
                SaveButtonIsDefault = true,
                Owner = this.GetWindow()
            };

            if (!w.ShowDialog() ?? false)
            {
                return;
            }

            if (selectorVm.SelectedItem is Device d)
            {
                this.Target = d.GetSidString();
                this.TargetObjectId = d.Id;
                this.CachedTargetName = d.DisplayName;
                this.TargetAuthorityId = selectedTenant;
                this.Type = type;
                this.DisplayName = d.DisplayName;
            }
            else if (selectorVm.SelectedItem is Group g)
            {
                this.Target = g.GetSidString();
                this.Type = type;
                this.TargetObjectId = g.Id;
                this.CachedTargetName = g.DisplayName;
                this.TargetAuthorityId = selectedTenant;
                this.DisplayName = g.DisplayName;
            }
        }

        private void ShowAdObjectSelector(TargetType type, string selectedForest)
        {
            string targetServer = this.discoveryServices.GetDomainController(selectedForest ?? this.discoveryServices.GetForestNameDns());

            if (type == TargetType.AdGroup)
            {
                if (this.objectSelectionProvider.GetGroup(this, targetServer, out SecurityIdentifier sid))
                {
                    this.Target = sid.ToString();
                    this.Type = type;

                    var group = this.directory.GetGroup(sid);
                    this.TargetObjectId = group.Guid.ToString();
                    this.TargetAuthorityId = this.discoveryServices.GetDomainNameDns(sid);
                    this.CachedTargetName = group.MsDsPrincipalName;
                    this.DisplayName = group.MsDsPrincipalName;

                }
            }
            else if (type == TargetType.AdComputer)
            {
                if (this.objectSelectionProvider.GetComputer(this, targetServer, out SecurityIdentifier sid))
                {
                    this.Target = sid.ToString();
                    this.Type = type;
                    var computer = this.directory.GetComputer(sid);
                    this.TargetObjectId = computer.Guid.ToString();
                    this.TargetAuthorityId = this.discoveryServices.GetDomainNameDns(sid);
                    this.CachedTargetName = computer.MsDsPrincipalName;
                    this.DisplayName = computer.MsDsPrincipalName;
                }
            }
        }

        private void ShowContainerDialog(TargetType type)
        {
            string path = this.Target ?? Domain.GetComputerDomain().GetDirectoryEntry().GetPropertyString("distinguishedName");

            string basePath = this.discoveryServices.GetFullyQualifiedRootAdsPath(path);
            string initialPath = this.discoveryServices.GetFullyQualifiedAdsPath(path);

            if (this.objectSelectionProvider.SelectContainer(this, "Select container", "Select container", basePath, initialPath, out string container))
            {
                this.Target = container;
                this.Type = type;
                this.TargetObjectId = null;
                this.CachedTargetName = null;
                this.DisplayName = container;
                this.TargetAuthorityId = this.discoveryServices.GetDomainNameDns(container);
            }
        }

        public UIElement View { get; set; }

        private async Task<string> GetTargetNameFromSidOrDefaultAsync(string sid)
        {
            if (string.IsNullOrWhiteSpace(sid))
            {
                return null;
            }

            try
            {
                SecurityIdentifier s = new SecurityIdentifier(sid);

                if (s.IsAmsAuthority())
                {
                    if (this.Type == TargetType.AmsComputer)
                    {
                        var device = await this.deviceProvider.GetDeviceAsync(this.TargetObjectId);
                        return device.Name;
                    }
                    else
                    {
                        var group = await this.amsGroupProvider.GetGroupBySid(this.Target);
                        return group.Name;
                    }
                }
                else if (s.IsAadAuthority())
                {
                    if (this.Type == TargetType.AadGroup)
                    {
                        var device = await this.graphProvider.GetAadGroupByIdAsync(this.TargetAuthorityId, this.TargetObjectId);
                        return device.DisplayName;
                    }
                    else if (this.Type == TargetType.AadComputer)
                    {
                        var group = await this.graphProvider.GetAadDeviceByIdAsync(this.TargetAuthorityId, this.TargetObjectId);
                        return group.DisplayName;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return await this.GetNameFromActiveDirectorySidOrDefaultAsync(s);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<string> GetNameFromActiveDirectorySidOrDefaultAsync(string sid)
        {
            return await Task.Run(() => this.GetNameFromActiveDirectorySidOrDefault(sid));
        }

        private async Task<string> GetNameFromActiveDirectorySidOrDefaultAsync(SecurityIdentifier sid)
        {
            return await Task.Run(() => this.GetNameFromActiveDirectorySidOrDefault(sid));
        }

        private string GetNameFromActiveDirectorySidOrDefault(string sid)
        {
            if (string.IsNullOrWhiteSpace(sid))
            {
                return null;
            }

            try
            {
                SecurityIdentifier s = new SecurityIdentifier(sid);
                return this.GetNameFromActiveDirectorySidOrDefault(s);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetNameFromActiveDirectorySidOrDefault(SecurityIdentifier sid)
        {
            try
            {
                return this.directory.TryGetPrincipal(sid, out IActiveDirectorySecurityPrincipal principal) ? principal.MsDsPrincipalName : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool SdHasMask(string securityDescriptor, AccessMask mask)
        {
            if (string.IsNullOrWhiteSpace(securityDescriptor))
            {
                return false;
            }

            try
            {
                RawSecurityDescriptor sd = new RawSecurityDescriptor(securityDescriptor);

                if (sd.DiscretionaryAcl == null)
                {
                    return false;
                }

                foreach (CommonAce ace in sd.DiscretionaryAcl.OfType<CommonAce>())
                {
                    if (ace.AceType == AceType.AccessAllowed && ((ace.AccessMask & (int)mask) == (int)mask))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(EventIDs.UIGenericWarning, ex, "Error processing security descriptor");
            }

            return false;
        }

        public IEnumerable<CommonAce> GetAceEntries()
        {
            if (this.IsModeScript || string.IsNullOrWhiteSpace(this.SecurityDescriptor))
            {
                return null;
            }

            try
            {
                RawSecurityDescriptor sd = new RawSecurityDescriptor(this.SecurityDescriptor);

                if (sd.DiscretionaryAcl == null)
                {
                    return null;
                }

                return sd.DiscretionaryAcl.OfType<CommonAce>();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(EventIDs.UIGenericWarning, ex, "Error processing security descriptor");
            }

            return null;
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public async Task LearnMoreLinkLapsHistory()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.EnterpriseEditionLearnMoreLinkLapsHistory);
        }

        public async Task LearnMoreLinkPowerShellAuthZ()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.EnterpriseEditionLearnMoreLinkPowerShellAuthz);
        }
    }
}