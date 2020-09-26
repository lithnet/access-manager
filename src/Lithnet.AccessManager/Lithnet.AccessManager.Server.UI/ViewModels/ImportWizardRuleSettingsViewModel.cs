using System;
using System.Security.Principal;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.AuthorizationRuleImport;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class ImportWizardRuleSettingsViewModel : Screen
    {
        private readonly IDirectory directory;
        private readonly ILogger<ImportWizardRuleSettingsViewModel> logger;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly INotificationChannelSelectionViewModelFactory notificationChannelFactory;
        private readonly IObjectSelectionProvider objectSelectionProvider;
        private readonly AuditNotificationChannels channels = new AuditNotificationChannels();

        public ImportWizardRuleSettingsViewModel(INotificationChannelSelectionViewModelFactory notificationChannelFactory, ILogger<ImportWizardRuleSettingsViewModel> logger, IDialogCoordinator dialogCoordinator, IModelValidator<ImportWizardRuleSettingsViewModel> validator, IDirectory directory, IObjectSelectionProvider objectSelectionProvider)
        {
            this.directory = directory;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.notificationChannelFactory = notificationChannelFactory;
            this.Validator = validator;
            this.objectSelectionProvider = objectSelectionProvider;
            _ = this.Initialize();
        }

        public async Task Initialize()
        {
            this.Notifications = notificationChannelFactory.CreateViewModel(this.channels);
            await this.ValidateAsync();
        }

        public void SetImportMode(ImportMode mode)
        {
            this.AllowLaps = false;
            this.AllowLapsHistory = false;
            this.AllowJit = false;
            this.AllowBitlocker = false;
            this.LapsEnabled = true;

            switch (mode)
            {
                case ImportMode.BitLocker:
                    this.AllowBitlocker = true;
                    break;

                case ImportMode.LapsWeb:
                    this.AllowLaps = true;
                    this.LapsEnabled = false;
                    break;

                case ImportMode.Laps:
                case ImportMode.CsvFile:
                    this.AllowLaps = true;
                    break;

                case ImportMode.Rpc:
                    this.AllowBitlocker = true;
                    break;
            }

            if (!(this.AllowJit || this.AllowLaps || this.AllowLapsHistory || this.AllowBitlocker))
            {
                this.AllowLaps = true;
            }
        }

        public NotificationChannelSelectionViewModel Notifications { get; private set; }

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

        public bool LapsEnabled { get; set; }

        public bool LapsSelectionVisible => this.AllowLaps && this.LapsEnabled;

        public async Task SelectJitGroup()
        {
            try
            {
                if (this.objectSelectionProvider.GetGroup(this, out SecurityIdentifier sid))
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
    }
}
