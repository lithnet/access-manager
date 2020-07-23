using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SecurityDescriptorTargetViewModel : PropertyChangedBase, IViewAware
    {
        private readonly IDirectory directory;

        private readonly ILogger<SecurityDescriptorTargetViewModel> logger;

        private readonly IDialogCoordinator dialogCoordinator;

        public SecurityDescriptorTarget Model { get; }

        public SecurityDescriptorTargetViewModel(SecurityDescriptorTarget model, INotificationChannelSelectionViewModelFactory notificationChannelFactory, IFileSelectionViewModelFactory fileSelectionViewModelFactory, IAppPathProvider appPathProvider, ILogger<SecurityDescriptorTargetViewModel> logger, IDialogCoordinator dialogCoordinator)
        {
            this.directory = new ActiveDirectory();
            this.Model = model;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;

            this.Script = fileSelectionViewModelFactory.CreateViewModel(model, () => model.Script, appPathProvider.ScriptsPath);
            this.Script.DefaultFileExtension = "ps1";
            this.Script.Filter = "PowerShell script|*.ps1";
            this.Script.NewFileContent = ScriptTemplates.AuthorizationScriptTemplate;
            this.Script.ShouldValidate = false;

            this.Notifications = notificationChannelFactory.CreateViewModel(model.Notifications);
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

        public string Target { get => this.Model.Target; set => this.Model.Target = value; }

        public FileSelectionViewModel Script { get; }

        public TargetType Type { get => this.Model.Type; set => this.Model.Type = value; }

        public string SecurityDescriptor { get => this.Model.SecurityDescriptor; set => this.Model.SecurityDescriptor = value; }

        public string Description { get => this.Model.Description; set => this.Model.Description = value; }

        public string JitAuthorizingGroup { get => this.Model.Jit.AuthorizingGroup; set => this.Model.Jit.AuthorizingGroup = value; }

        public string EvaluationDomain { get; set; }

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

        public string DisplayName => this.Type == TargetType.Container ? this.Target : $"{this.TryGetNameFromSid(this.Target)} ({this.Type})";

        public bool ShowLapsOptions => this.IsModeScript || SdHasMask(this.SecurityDescriptor, AccessMask.Laps);

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
                    new SiAccess(0x00000200, "Laps access", InheritFlags.SiAccessGeneral),
                    new SiAccess(0x00000400, "Laps history", InheritFlags.SiAccessGeneral),
                    new SiAccess(0x00000800, "Just-in-time access", InheritFlags.SiAccessGeneral),
                    new SiAccess(0, "None", 0)
                };

                this.SecurityDescriptor ??= "O:SYD:";

                RawSecurityDescriptor sd = new RawSecurityDescriptor(this.SecurityDescriptor);

                string targetServer = null;

                if (this.Type == TargetType.Container)
                {
                    targetServer = this.directory.GetDomainNameDnsFromDn(this.Target);
                }
                else
                {
                    if (this.Target.TryParseAsSid(out SecurityIdentifier sid))
                    {
                        targetServer = this.directory.GetDomainNameDnsFromSid(sid);
                    }
                }

                SiObjectInfoFlags flags = SiObjectInfoFlags.EditPermissions;

                GenericMapping mapping = new GenericMapping();
                mapping.GenericAll = 0x200 | 0x400 | 0x800;

                BasicSecurityInformation info = new BasicSecurityInformation(
                    flags,
                    this.DisplayName,
                    rights,
                    sd,
                    mapping,
                    targetServer
                );

                ISecurityInformation d = info;

                if (NativeMethods.EditSecurity(this.GetHandle(), info))
                {
                    info.SecurityDescriptor.Owner = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                    info.SecurityDescriptor.Group = null;
                    this.SecurityDescriptor = info.SecurityDescriptor.GetSddlForm(AccessControlSections.All);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Edit security error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the object security\r\n{ex.Message}");
            }
        }

        public bool CanSelectJitGroup => this.CanEdit;

        public async Task SelectJitGroup()
        {
            try
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
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Select JIT group error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
            }
        }

        public async Task SelectTarget()
        {
            try
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
                    if (trust.TrustDirection == TrustDirection.Inbound ||
                        trust.TrustDirection == TrustDirection.Bidirectional)
                    {
                        vm.AvailableForests.Add(trust.TargetName);
                    }
                }

                await this.GetWindow().ShowChildWindowAsync(w);

                if (w.Result != MessageDialogResult.Affirmative)
                {
                    return;
                }

                this.Type = vm.TargetType;

                if (vm.TargetType == TargetType.Container)
                {
                    var container =
                        NativeMethods.ShowContainerDialog(this.GetHandle(), "Select domain", "Select domain");
                    if (container != null)
                    {
                        this.Target = container;
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

                    string targetServer = vm.SelectedForest == domain.Forest.Name ? null : vm.SelectedForest;
                    var result = NativeMethods
                        .ShowObjectPickerDialog(this.GetHandle(), targetServer, scope, "objectClass", "objectSid")
                        .FirstOrDefault();

                    if (result != null)
                    {
                        byte[] sid = result.Attributes["objectSid"] as byte[];
                        if (sid == null)
                        {
                            return;
                        }

                        this.Target = new SecurityIdentifier(sid, 0).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Select target error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
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
                    if (ace.AceType == AceType.AccessAllowed && ((ace.AccessMask & (int) mask) == (int) mask))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Error processing security descriptor");
            }

            return false;
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }
    }
}