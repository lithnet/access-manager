using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Community.Windows.Forms;
using Lithnet.AccessManager.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Stylet;
using System.IO;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetViewModel : PropertyChangedBase, IViewAware
    {
        private readonly INotificationSubscriptionProvider subscriptions;

        private readonly IEventAggregator eventAggregator;

        private readonly IDirectory directory;

        private readonly IDialogCoordinator dialogCoordinator;

        public SecurityDescriptorTarget Model { get; }

        public SecurityDescriptorTargetViewModel(SecurityDescriptorTarget model, IDialogCoordinator dialogCoordinator, INotificationSubscriptionProvider subscriptionProvider, IEventAggregator eventAggregator)
        {
            this.directory = new ActiveDirectory();
            this.Model = model;
            this.subscriptions = subscriptionProvider;
            this.eventAggregator = eventAggregator;
            this.dialogCoordinator = dialogCoordinator;

            this.Script = new FileSelectionViewModel(model, () => model.Script, AppPathProvider.ScriptsPath, dialogCoordinator);
            this.Script.DefaultFileExtension = "ps1";
            this.Script.Filter = "PowerShell script|*.ps1";
            this.Script.NewFileContent = ScriptTemplates.AuthorizationScriptTemplate;
            this.Script.ShouldValidate = false;

            this.Notifications = new NotificationChannelSelectionViewModel(this.Model.Notifications, subscriptionProvider, eventAggregator);
        }

        public NotificationChannelSelectionViewModel Notifications { get; }

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

        public string Id { get => this.Model.Id; set => this.Model.Id = value; }

        public FileSelectionViewModel Script { get; }

        public TargetType Type { get => this.Model.Type; set => this.Model.Type = value; }

        public string SecurityDescriptor { get => this.Model.SecurityDescriptor; set => this.Model.SecurityDescriptor = value; }

        public string JitAuthorizingGroup { get => this.Model.Jit.AuthorizingGroup; set => this.Model.Jit.AuthorizingGroup = value; }

        public string JitGroupDisplayName
        {
            get
            {
                return this.TryGetNameIfSid(this.JitAuthorizingGroup);
            }
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

        public int LapsExpireMinutes { get => (int)this.LapsExpireAfter.TotalMinutes; set => this.LapsExpireAfter = new TimeSpan(0, value, 0); }

        public bool ExpireLapsPassword
        {
            get => this.LapsExpireAfter.TotalSeconds > 0; set
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

        public int JitExpireMinutes { get => (int)this.JitExpireAfter.TotalMinutes; set => this.JitExpireAfter = new TimeSpan(0, value, 0); }

        public PasswordStorageLocation RetrievalLocation { get => this.Model.Laps.RetrievalLocation; set => this.Model.Laps.RetrievalLocation = value; }

        public string DisplayName => this.Type == TargetType.Container ? this.Id : this.TryGetNameFromSid(this.Id);

        public bool ShowLapsOptions => this.IsModeScript || SdHasMask(this.SecurityDescriptor, AccessMask.Laps);

        public bool ShowJitOptions => this.IsModeScript || SdHasMask(this.SecurityDescriptor, AccessMask.Jit);

        public bool CanEdit => this.Id != null;

        public bool CanEditPermissions => this.CanEdit && this.AuthorizationMode == AuthorizationMode.SecurityDescriptor;

        public bool CanSelectScript => this.CanEdit && this.AuthorizationMode == AuthorizationMode.PowershellScript;

        public void EditPermissions()
        {
            AccessControlEditorDialog dialog = new AccessControlEditorDialog();

            dialog.PageType = Community.Security.AccessControl.SecurityPageType.BasicPermissions;
            dialog.AllowEditOwner = false;
            dialog.AllowEditAudit = false;
            dialog.AllowDaclInheritanceReset = false;
            dialog.AllowSaclInheritanceReset = false;
            dialog.ViewOnly = false;

            if (this.SecurityDescriptor == null)
            {
                this.SecurityDescriptor = "O:SYD:";
            }

            var provider = new AdminAccessTargetProvider();
            RawSecurityDescriptor sd = new RawSecurityDescriptor(this.SecurityDescriptor);
            byte[] sdBytes = new byte[sd.BinaryLength];
            sd.GetBinaryForm(sdBytes, 0);
            dialog.Initialize(this.DisplayName, this.DisplayName, false, provider, sdBytes);

            var r = dialog.ShowDialog();

            if (r == System.Windows.Forms.DialogResult.OK)
            {
                RawSecurityDescriptor rsd = new RawSecurityDescriptor(dialog.SDDL);
                rsd.Owner = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                rsd.Group = null;
                this.SecurityDescriptor = rsd.GetSddlForm(AccessControlSections.All);
            }
        }

        public bool CanSelectJitGroup => this.CanEdit;

        public void SelectJitGroup()
        {
            ExternalDialogWindow w = new ExternalDialogWindow();
            w.Title = "Select forest";
            var vm = new SelectForestViewModel();
            w.DataContext = vm;
            w.SaveButtonName = "Next...";
            w.SaveButtonIsDefault = true;
            vm.AvailableForests = new List<string>();
            var domain = Domain.GetCurrentDomain();
            vm.AvailableForests.Add(domain.Forest.Name);
            vm.SelectedForest = domain.Forest.Name;

            foreach (var trust in domain.Forest.GetAllTrustRelationships().OfType<TrustRelationshipInformation>())
            {
                if (trust.TrustDirection == TrustDirection.Inbound || trust.TrustDirection == TrustDirection.Bidirectional)
                {
                    vm.AvailableForests.Add(trust.TargetName);
                }
            }

            w.Owner = this.GetWindow();

            if (!w.ShowDialog() ?? false)
            {
                return;
            }

            DsopScopeInitInfo scope = new DsopScopeInitInfo();
            scope.Filter = new DsFilterFlags();

            scope.Filter.UpLevel.BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE;
            scope.ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN;
            scope.InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE;

            string target = vm.SelectedForest == domain.Forest.Name ? null : vm.SelectedForest;

            var result = NativeMethods.ShowObjectPickerDialog(this.GetHandle(), target, scope, "objectClass", "objectSid").FirstOrDefault();

            if (result != null)
            {
                byte[] sid = result.Attributes["objectSid"] as byte[];
                if (sid == null)
                {
                    return;
                }

                this.JitAuthorizingGroup = new SecurityIdentifier(sid, 0).ToString();
            }
        }

        public async Task SelectTarget()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Select target type";
            var vm = new SelectTargetTypeViewModel();
            w.DataContext = vm;
            w.SaveButtonName = "Next...";
            w.SaveButtonIsDefault = true;
            vm.TargetType = this.Type;
            vm.AvailableForests = new List<string>();
            var domain = Domain.GetCurrentDomain();
            vm.AvailableForests.Add(domain.Forest.Name);
            vm.SelectedForest = domain.Forest.Name;

            foreach (var trust in domain.Forest.GetAllTrustRelationships().OfType<TrustRelationshipInformation>())
            {
                if (trust.TrustDirection == TrustDirection.Inbound || trust.TrustDirection == TrustDirection.Bidirectional)
                {
                    vm.AvailableForests.Add(trust.TargetName);
                }
            }

            await ChildWindowManager.ShowChildWindowAsync(this.GetWindow(), w);

            if (w.Result != MessageDialogResult.Affirmative)
            {
                return;
            }

            this.Type = vm.TargetType;

            if (vm.TargetType == TargetType.Container)
            {
                var container = NativeMethods.ShowContainerDialog(this.GetHandle(), "Select domain", "Select domain");
                if (container != null)
                {
                    this.Id = container;
                }
            }
            else
            {
                DsopScopeInitInfo scope = new DsopScopeInitInfo();
                scope.Filter = new DsFilterFlags();
                if (vm.TargetType == TargetType.Computer)
                {
                    scope.Filter.UpLevel.BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_COMPUTERS;
                }
                else
                {
                    scope.Filter.UpLevel.BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE;
                }

                scope.ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN;

                scope.InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_COMPUTERS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE;

                string targetServer = vm.SelectedForest == domain.Forest.Name ? null : vm.SelectedForest;
                var result = NativeMethods.ShowObjectPickerDialog(this.GetHandle(), targetServer, scope, "objectClass", "objectSid").FirstOrDefault();

                if (result != null)
                {
                    byte[] sid = result.Attributes["objectSid"] as byte[];
                    if (sid == null)
                    {
                        return;
                    }

                    this.Id = new SecurityIdentifier(sid, 0).ToString();
                }
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

        private static bool SdHasMask(string securityDescriptor, AccessMask mask)
        {
            if (string.IsNullOrWhiteSpace(securityDescriptor))
            {
                return false;
            }

            try
            {
                RawSecurityDescriptor sd = new RawSecurityDescriptor(securityDescriptor);
                foreach (var ace in sd.DiscretionaryAcl.OfType<CommonAce>())
                {
                    if (ace.AceType == AceType.AccessAllowed && ((ace.AccessMask & (int)mask) == (int)mask))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }
    }
}