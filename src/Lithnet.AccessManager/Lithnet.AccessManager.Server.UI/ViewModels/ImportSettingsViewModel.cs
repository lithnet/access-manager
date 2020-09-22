using System;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Lithnet.AccessManager.Server.UI.Providers;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PropertyChanged;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class ImportSettingsViewModel : Screen, IHelpLink
    {
        private readonly IDirectory directory;
        private readonly ILogger<ImportSettingsViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly INotificationChannelSelectionViewModelFactory notificationChannelFactory;
        private readonly IDomainTrustProvider domainTrustProvider;
        private readonly IDiscoveryServices discoveryServices;
        private readonly IShellExecuteProvider shellExecuteProvider;

        public ImportSettingsViewModel(INotificationChannelSelectionViewModelFactory notificationChannelFactory, IFileSelectionViewModelFactory fileSelectionViewModelFactory, IAppPathProvider appPathProvider, ILogger<ImportSettingsViewModel> logger, IDialogCoordinator dialogCoordinator, IModelValidator<ImportSettingsViewModel> validator, IDirectory directory, IDomainTrustProvider domainTrustProvider, IDiscoveryServices discoveryServices, IShellExecuteProvider shellExecuteProvider)
        {
            this.directory = directory;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.notificationChannelFactory = notificationChannelFactory;
            this.Validator = validator;
            this.domainTrustProvider = domainTrustProvider;
            this.discoveryServices = discoveryServices;
            this.shellExecuteProvider = shellExecuteProvider;
            _ = this.Initialize();
        }

        public async Task Initialize()
        {
            this.Notifications = notificationChannelFactory.CreateViewModel(this.channels);
            await this.ValidateAsync();
        }

        private readonly AuditNotificationChannels channels = new AuditNotificationChannels();

        public NotificationChannelSelectionViewModel Notifications { get; private set; }

        public string Target { get; set; }

        public string Description { get; set; }

        public string JitAuthorizingGroup { get; set; }

        public string JitGroupDisplayName
        {
            get => this.TryGetNameIfSid(this.JitAuthorizingGroup);
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

        public TimeSpan JitExpireAfter { get; set; }

        public TimeSpan LapsExpireAfter { get; set; }

        public int LapsExpireMinutes
        {
            get => (int)this.LapsExpireAfter.TotalMinutes;
            set => this.LapsExpireAfter = new TimeSpan(0, Math.Max(value, 15), 0);
        }

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

        public int JitExpireMinutes
        {
            get => (int)this.JitExpireAfter.TotalMinutes;
            set => this.JitExpireAfter = new TimeSpan(0, Math.Max(value, 15), 0);
        }

        [AlsoNotifyFor(nameof(AllowLaps), nameof(AllowJit), nameof(AllowBitlocker), nameof(AllowLapsHistory))]
        public bool AllowLaps { get; set; } = true;

        [AlsoNotifyFor(nameof(AllowLaps), nameof(AllowJit), nameof(AllowBitlocker), nameof(AllowLapsHistory), nameof(JitAuthorizingGroup))]
        public bool AllowJit { get; set; }

        [AlsoNotifyFor(nameof(AllowLaps), nameof(AllowJit), nameof(AllowBitlocker), nameof(AllowLapsHistory))]
        public bool AllowLapsHistory { get; set; }

        [AlsoNotifyFor(nameof(AllowLaps), nameof(AllowJit), nameof(AllowBitlocker), nameof(AllowLapsHistory))]
        public bool AllowBitlocker { get; set; }

        public string ImportFile { get; set; }

        [AlsoNotifyFor(nameof(ImportFile))]
        public UserDiscoveryMode ImportType { get; set; } = UserDiscoveryMode.Laps;

        public bool ImportTypeLaps
        {
            get => this.ImportType == UserDiscoveryMode.Laps;
            set
            {
                if (value)
                {
                    this.ImportType = UserDiscoveryMode.Laps;
                    this.AllowLaps = true;
                }
            }
        }

        public bool ImportTypeBitLocker
        {
            get => this.ImportType == UserDiscoveryMode.BitLocker;
            set
            {
                if (value)
                {
                    this.ImportType = UserDiscoveryMode.BitLocker;
                    this.AllowBitlocker = true;
                }
            }
        }

        public bool ImportTypeLocalAdmins
        {
            get => this.ImportType == UserDiscoveryMode.LocalAdminRpc;
            set
            {
                if (value)
                {
                    this.ImportType = UserDiscoveryMode.LocalAdminRpc;
                    this.AllowJit = true;
                }
            }
        }

        public bool ImportTypeFile
        {
            get => this.ImportType == UserDiscoveryMode.File;
            set
            {
                if (value)
                {
                    this.ImportType = UserDiscoveryMode.File;
                    this.AllowJit = true;
                }
            }
        }

        public bool DoNotConsolidate { get; set; }

        public bool DoNotConsolidateOnError { get; set; }

        public bool DoNotConsolidateOnErrorEnabled => !this.DoNotConsolidate;

        private string GetDcForTargetOrDefault()
        {
            if (this.Target == null)
            {
                return this.discoveryServices.GetDomainNameDns();
            }

            string domain = null;

            try
            {
                domain = this.discoveryServices.GetDomainNameDns(this.Target);

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

            string forest = null;
            try
            {
                forest = discoveryServices.GetForestNameDns(this.Target);

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
                SecurityIdentifier sid = ShowObjectPickerGroups(this.GetDcForTargetOrDefault());
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

            DsopResult result = NativeMethods.ShowObjectPickerDialog(this.GetHandle(), targetServer, scope, "objectClass", "objectSid").FirstOrDefault();

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

        public async Task HelpRpcLocalAdmin()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.HelpLinkImportLocalAdmins);
        }

        public async Task HelpCsvFileFormat()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.HelpLinkImportCsv);
        }

        public async Task SelectImportFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.DefaultExt = "csv";
            openFileDialog.DereferenceLinks = true;
            openFileDialog.Filter = "CSV File (*.csv)|*.csv";
            openFileDialog.Multiselect = false;

            if (!string.IsNullOrWhiteSpace(this.ImportFile))
            {
                try
                {
                    openFileDialog.InitialDirectory = Path.GetDirectoryName(this.ImportFile) ?? string.Empty;
                    openFileDialog.FileName = Path.GetFileName(this.ImportFile) ?? string.Empty;
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(EventIDs.UIGenericWarning, ex, "Could not determine file path");
                }
            }

            if (openFileDialog.ShowDialog(this.GetWindow()) == false)
            {
                return;
            }

            foreach (var line in File.ReadLines(openFileDialog.FileName))
            {
                if (line.Count(t => t == ',') != 1)
                {
                    await dialogCoordinator.ShowMessageAsync(this, "File format error", "The file was not in the expected format. View the help topic for this page for information on the correct format");
                    return;
                }
            }

            this.ImportFile = openFileDialog.FileName;
        }

        public bool CanImport => this.ImportFile != null && File.Exists(this.ImportFile) && !string.IsNullOrWhiteSpace(this.Target) && (this.AllowBitlocker || this.AllowJit || this.AllowLaps || this.AllowLapsHistory);

        public async Task SelectTarget()
        {
            try
            {
                ShowContainerDialog();
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Select target error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
            }
        }

        private void ShowContainerDialog()
        {
            string path = this.Target ?? Domain.GetComputerDomain().GetDirectoryEntry().GetPropertyString("distinguishedName");

            string basePath = this.discoveryServices.GetFullyQualifiedDomainControllerAdsPath(path);
            string initialPath = this.discoveryServices.GetFullyQualifiedAdsPath(path);

            string container = NativeMethods.ShowContainerDialog(this.GetHandle(), "Select container", "Select container", basePath, initialPath);

            if (container != null)
            {
                this.Target = container;
            }
        }

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
                if (this.directory.TryGetPrincipal(s, out ISecurityPrincipal principal))
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

        public string HelpLink { get; set; }
    }
}
