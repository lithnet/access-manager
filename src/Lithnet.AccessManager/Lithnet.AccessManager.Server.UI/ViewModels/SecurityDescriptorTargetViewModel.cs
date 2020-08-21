using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class SecurityDescriptorTargetViewModel : ValidatingModelBase, IViewAware
    {
        private static readonly Domain currentDomain = Domain.GetCurrentDomain();
        private static readonly Forest currentForest = Forest.GetCurrentForest();
        
        private readonly IDirectory directory;
        private readonly ILogger<SecurityDescriptorTargetViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly INotificationChannelSelectionViewModelFactory notificationChannelFactory;
        private readonly IDomainTrustProvider domainTrustProvider;

        public SecurityDescriptorTarget Model { get; }

        public SecurityDescriptorTargetViewModel(SecurityDescriptorTarget model, INotificationChannelSelectionViewModelFactory notificationChannelFactory, IFileSelectionViewModelFactory fileSelectionViewModelFactory, IAppPathProvider appPathProvider, ILogger<SecurityDescriptorTargetViewModel> logger, IDialogCoordinator dialogCoordinator, IModelValidator<SecurityDescriptorTargetViewModel> validator, IDirectory directory, IDomainTrustProvider domainTrustProvider)
        {
            this.directory = directory;
            this.Model = model;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.notificationChannelFactory = notificationChannelFactory;
            this.Validator = validator;
            this.domainTrustProvider = domainTrustProvider;

            this.Script = fileSelectionViewModelFactory.CreateViewModel(model, () => model.Script, appPathProvider.ScriptsPath);
            this.Script.DefaultFileExtension = "ps1";
            this.Script.Filter = "PowerShell script|*.ps1";
            this.Script.NewFileContent = ScriptTemplates.AuthorizationScriptTemplate;
            this.Script.ShouldValidate = false;
            this.Script.PropertyChanged += Script_PropertyChanged;
        }

        public async Task Initialize()
        {
            this.Notifications = notificationChannelFactory.CreateViewModel(this.Model.Notifications);
            await this.ValidateAsync();
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

        public bool IsModePermission { get => this.AuthorizationMode == AuthorizationMode.SecurityDescriptor; set => this.AuthorizationMode = value ? AuthorizationMode.SecurityDescriptor : AuthorizationMode.PowershellScript; }

        public bool IsModeScript { get => this.AuthorizationMode == AuthorizationMode.PowershellScript; set => this.AuthorizationMode = value ? AuthorizationMode.PowershellScript : AuthorizationMode.SecurityDescriptor; }

        public string Target { get => this.Model.Target; set => this.Model.Target = value; }

        public FileSelectionViewModel Script { get; }

        public TargetType Type { get => this.Model.Type; set => this.Model.Type = value; }

        public string SecurityDescriptor { get => this.Model.SecurityDescriptor; set => this.Model.SecurityDescriptor = value; }

        public string Description { get => this.Model.Description; set => this.Model.Description = value; }

        public string JitAuthorizingGroup { get => this.Model.Jit.AuthorizingGroup; set => this.Model.Jit.AuthorizingGroup = value; }

        public string JitGroupDisplayName
        {
            get => this.TryGetNameIfSid(this.JitAuthorizingGroup);
            set
            {
                if (value.Contains("{computerName}", StringComparison.OrdinalIgnoreCase) || value.Contains("{computerDomain}", StringComparison.OrdinalIgnoreCase))
                {
                    this.JitAuthorizingGroup = value;
                }
                else
                {
                    if (this.directory.TryGetGroup(value, out IGroup group))
                    {
                        this.JitAuthorizingGroup = group.Sid.ToString();
                    }
                    else
                    {
                        this.JitAuthorizingGroup = value;
                    }
                }
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

        public string DisplayName => this.Target == null ? null : this.Type == TargetType.Container ? this.Target : $"{this.TryGetNameFromSid(this.Target)} ({this.Type})";

        public bool ShowLapsOptions => this.IsModeScript || SdHasMask(this.SecurityDescriptor, AccessMask.LocalAdminPassword);

        public bool ShowJitOptions => this.IsModeScript || SdHasMask(this.SecurityDescriptor, AccessMask.Jit);

        public bool CanEdit => this.Target != null;

        public bool CanEditPermissions => this.CanEdit && this.AuthorizationMode == AuthorizationMode.SecurityDescriptor && this.Target != null;

        public bool CanSelectScript => this.CanEdit && this.AuthorizationMode == AuthorizationMode.PowershellScript;

        public async Task EditPermissions()
        {
            try
            {
                var rights = new List<SiAccess>
                {
                    new SiAccess((uint)AccessMask.LocalAdminPassword, "Local admin password", InheritFlags.SiAccessGeneral),
                    new SiAccess((uint)AccessMask.LocalAdminPasswordHistory, "Local admin password history", InheritFlags.SiAccessGeneral),
                    new SiAccess((uint)AccessMask.Jit, "Just-in-time access", InheritFlags.SiAccessGeneral),
                    new SiAccess((uint)AccessMask.BitLocker, "BitLocker recovery passwords", InheritFlags.SiAccessGeneral),
                };

                this.SecurityDescriptor ??= "O:SYD:";

                RawSecurityDescriptor sd = new RawSecurityDescriptor(this.SecurityDescriptor);

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

        public bool CanSelectJitGroup => this.CanEdit;

        private string GetDcForTargetOrDefault()
        {
            if (this.Target == null)
            {
                return currentDomain.Name;
            }

            string domain = null;

            try
            {
                if (this.Type == TargetType.Container)
                {
                    domain = this.directory.GetDomainNameDnsFromDn(this.Target);
                }
                else if (this.Target.TryParseAsSid(out SecurityIdentifier sid))
                {
                    domain = this.directory.GetDomainNameDnsFromSid(sid);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(EventIDs.UIGenericWarning, ex, "Error dc for target");
            }

            return this.directory.GetDomainControllerForDomain(domain ?? currentDomain.Name);
        }

        private string GetForestDcForTargetOrDefault()
        {
            return this.directory.GetDomainControllerForDomain(GetForestForTargetOrDefault() ?? currentForest.Name);
        }

        private string GetForestForTargetOrDefault()
        {
            if (string.IsNullOrWhiteSpace(this.Target))
            {
                return currentForest.Name;
            }

            string forest = null;
            try
            {
                if (this.Type == TargetType.Container)
                {
                    forest = directory.GetForestDnsNameForOU(this.Target);
                }
                else if (this.Target.TryParseAsSid(out SecurityIdentifier sid))
                {
                    forest = Domain.GetDomain(new DirectoryContext(DirectoryContextType.Domain, this.directory.GetDomainNameDnsFromSid(sid))).Forest.Name;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(EventIDs.UIGenericWarning, ex, "Error resolving forest name");
            }

            forest ??= currentForest.Name;

            return forest;
        }


        public async Task SelectJitGroup()
        {
            try
            {
                //var vm = new SelectForestViewModel
                //{
                //    AvailableForests = BuildAvailableForests(),
                //    SelectedForest = this.GetForestForTargetOrDefault()
                //};

                //ExternalDialogWindow w = new ExternalDialogWindow
                //{
                //    Title = "Select forest",
                //    DataContext = vm,
                //    SizeToContent = SizeToContent.WidthAndHeight,
                //    SaveButtonName = "Next...",
                //    SaveButtonIsDefault = true,
                //    Owner = this.GetWindow()
                //};

                //if (!w.ShowDialog() ?? false)
                //{
                //    return;
                //}

                var sid = ShowObjectPickerGroups(this.GetDcForTargetOrDefault());
                if (sid != null)
                {
                    this.JitAuthorizingGroup = sid.ToString();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Select JIT group error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
            }
        }

        private SecurityIdentifier ShowObjectPickerGroups(string targetServer)
        {
            DsopScopeInitInfo scope = new DsopScopeInitInfo();
            scope.Filter = new DsFilterFlags();
            scope.Filter.UpLevel.BothModeFilter =
                DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE |
                DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE |
                DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE;

            scope.ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN |
                              DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE |
                              DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN;

            scope.InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS |
                             DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE;

            var result = NativeMethods.ShowObjectPickerDialog(this.GetHandle(), targetServer, scope, "objectClass", "objectSid").FirstOrDefault();

            if (result != null)
            {
                byte[] sid = result.Attributes["objectSid"] as byte[];
                if (sid == null)
                {
                    return null;
                }

                return new SecurityIdentifier(sid, 0);
            }

            return null;
        }

        public async Task SelectTarget()
        {
            try
            {
                var vm = new SelectTargetTypeViewModel
                {
                    TargetType = this.Type,
                    AvailableForests = this.domainTrustProvider.GetForests().Select(t => t.Name).ToList()
                };

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

                this.Type = vm.TargetType;

                if (vm.TargetType == TargetType.Container)
                {
                    ShowContainerDialog();
                }
                else
                {
                    var sid = ShowObjectPickerDialogComputersAndGroups(vm.TargetType, vm.SelectedForest);

                    if (sid != null)
                    {
                        this.Target = sid.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Select target error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
            }
        }

        private SecurityIdentifier ShowObjectPickerDialogComputersAndGroups(TargetType targetType, string forest)
        {
            DsopScopeInitInfo scope = new DsopScopeInitInfo
            {
                Filter = new DsFilterFlags()
            };

            if (targetType == TargetType.Computer)
            {
                scope.Filter.UpLevel.BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_COMPUTERS;
            }
            else
            {
                scope.Filter.UpLevel.BothModeFilter =
                    DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE |
                    DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE |
                    DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE;
            }

            scope.ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN |
                              DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE |
                              DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN;

            scope.InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_COMPUTERS |
                             DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS |
                             DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE;

            string targetServer = this.directory.GetDomainControllerForDomain(forest ?? currentForest.Name);

            var result = NativeMethods.ShowObjectPickerDialog(this.GetHandle(), targetServer, scope, "objectClass", "objectSid").FirstOrDefault();

            if (result != null)
            {
                byte[] sid = result.Attributes["objectSid"] as byte[];
                if (sid == null)
                {
                    return null;
                }

                return new SecurityIdentifier(sid, 0);
            }

            return null;
        }

        private void ShowContainerDialog()
        {
            string path = this.Target ?? currentDomain.GetDirectoryEntry().GetPropertyString("distinguishedName");

            string basePath = this.directory.GetFullyQualifiedDomainControllerAdsPath(path);
            string initialPath = this.directory.GetFullyQualifiedAdsPath(path);

            var container = NativeMethods.ShowContainerDialog(this.GetHandle(), "Select container", "Select container", basePath, initialPath);

            if (container != null)
            {
                this.Target = container;
            }
        }

        public UIElement View { get; set; }

        private string TryGetNameFromSid(string sid)
        {
            if (string.IsNullOrWhiteSpace(sid))
            {
                return null;
            }

            try
            {
                if (sid.TryParseAsSid(out SecurityIdentifier s))
                {
                    if (this.directory.TryGetPrincipal(s, out ISecurityPrincipal principal))
                    {
                        return principal.MsDsPrincipalName;
                    }
                }

                return sid;
            }
            catch (Exception)
            {
                return "<invalid SID>";
            }
        }

        private string TryGetNameIfSid(string sid)
        {
            if (string.IsNullOrWhiteSpace(sid))
            {
                return null;
            }

            try
            {
                SecurityIdentifier s = new SecurityIdentifier(sid);
                if (this.directory.TryGetPrincipal(sid, out ISecurityPrincipal principal))
                {
                    return principal.MsDsPrincipalName;
                }
                else
                {
                    return sid;
                }
            }
            catch (Exception)
            {
                return sid;
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

                foreach (var ace in sd.DiscretionaryAcl.OfType<CommonAce>())
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

        public void AttachView(UIElement view)
        {
            this.View = view;
        }
    }
}