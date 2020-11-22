using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class LicensingViewModel : Screen, IHelpLink
    {
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly ILicenseManager licenseManager;
        private readonly ILogger<LicensingViewModel> logger;
        private readonly LicensingOptions licensingOptions;
        private readonly ILicenseDataProvider licenseDataProvider;
        private readonly IEventAggregator eventAggregator;

        public LicensingViewModel(IDialogCoordinator dialogCoordinator, IShellExecuteProvider shellExecuteProvider, ILicenseManager licenseManager, ILogger<LicensingViewModel> logger, LicensingOptions licensingOptions, INotifyModelChangedEventPublisher eventPublisher, ILicenseDataProvider licenseDataProvider, IEventAggregator eventAggregator)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.licenseManager = licenseManager;
            this.logger = logger;
            this.licensingOptions = licensingOptions;
            this.licenseDataProvider = licenseDataProvider;
            this.eventAggregator = eventAggregator;
            this.dialogCoordinator = dialogCoordinator;
            this.DisplayName = "License";
            this.ValidationResult = licenseManager.ValidateLicense(licenseDataProvider.GetRawLicenseData());
            eventPublisher.Register(this);
        }

        public string HelpLink => Constants.HelpLinkPageBitLocker;

        public PackIconMaterialKind Icon => PackIconMaterialKind.License;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }

        public ILicenseData License => this.ValidationResult?.License;

        public LicenseValidationResult ValidationResult { get; set; }

        public string LicenseStatusText
        {
            get
            {
                switch (this.ValidationResult.State)
                {
                    case LicenseState.NoLicensePresent:
                        return "Not licensed";

                    case LicenseState.Licensed:
                        if (this.IsEvaluationLicense)
                        {
                            if (this.IsLicenseExpiring90Days)
                            {
                                return $"Evaluation period expires in {this.ExpiryDaysRemaining}";
                            }
                            else
                            {
                                return "Active (evaluation license)";
                            }
                        }
                        else
                        {
                            if (this.IsLicenseExpiring90Days)
                            {
                                return $"Expires in {this.ExpiryDaysRemaining}";
                            }
                            else
                            {
                                return "Active";
                            }
                        }

                    case LicenseState.Invalid:
                        return $"Invalid - {this.ValidationResult.Message}";

                    case LicenseState.Expired:
                        return $"Expired";
                }

                return null;
            }
        }

        [NotifyModelChangedProperty]
        public string LicenseData
        {
            get => this.licensingOptions.Data;
            set => this.licensingOptions.Data = value;
        }

        public bool HasActiveLicenseNotExpiring => this.HasActiveLicense && !this.IsLicenseExpiring90Days;

        public bool HasActiveLicense => this.ValidationResult.State == LicenseState.Licensed;

        public bool IsEvaluationLicense => this.ValidationResult.License?.Type == LicenseType.Evaluation;

        public bool ShowLicenseInformation => this.ValidationResult.State != LicenseState.NoLicensePresent;

        public bool IsLicenseInvalidOrExpired => this.ValidationResult.State == LicenseState.Invalid || this.ValidationResult.State == LicenseState.Expired;

        public bool IsLicenseInvalid => this.ValidationResult.State == LicenseState.Invalid;

        public bool IsLicenseExpired => this.ValidationResult.State == LicenseState.Expired;

        public bool IsLicenseExpiring90Days => this.ValidationResult.State == LicenseState.Licensed && (this.License.ValidTo - DateTime.UtcNow).TotalDays <= 90;

        public bool IsLicenseExpiring30Days => this.ValidationResult.State == LicenseState.Licensed && (this.License.ValidTo - DateTime.UtcNow).TotalDays <= 30;

        public bool IsLicenseExpiredOrExpiring => this.IsLicenseExpired || this.IsLicenseExpiring90Days;

        public bool IsUnlicensed => this.ValidationResult.State == LicenseState.NoLicensePresent;

        public string Issued => this.License?.Issued.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        public string ValidTo => this.License?.ValidTo.ToLocalTime().ToString(CultureInfo.CurrentCulture);

        public bool HasExpired => this.License?.ValidTo > DateTime.UtcNow;

        public string Type => this.License?.Type.ToString();

        public string ExpiryDaysRemaining => this.License == null ? null : ToRelativeDate(this.License.ValidTo);

        public string LicensedUsers => this.License?.Units == null ? null : this.License.Units < 0 ? "Unlimited" : this.License.Units.ToString();

        public string LicensedTo => this.License?.LicensedTo;

        public string LicensedForests
        {
            get
            {
                if (this.License?.Audience == null)
                {
                    return null;
                }

                if (this.License.Audience.Any(t => t == "*"))
                {
                    return "All";
                }

                return string.Join(", ", this.License.Audience);
            }
        }

        public string KeyId => this.License?.KeyId;

        public string Product => this.License?.Product;

        public string EffectiveProductEdition => this.ValidationResult.EffectiveEdition == Enterprise.ProductEdition.Enterprise ? "Enterprise" : "Standard";

        public string ProductEdition => this.License == null ? null : this.License.ProductEdition == Enterprise.ProductEdition.Enterprise ? "Enterprise" : "Standard";

        public string Units => this.License?.Units.ToString();

        public bool HasEnterpriseLicense => this.ValidationResult.EffectiveEdition == Enterprise.ProductEdition.Enterprise;

        public bool HasStandardLicense => !this.HasEnterpriseLicense;

        public string Versions
        {
            get
            {
                if (this.License == null)
                {
                    return null;
                }

                if (this.License.ProductVersionMin == this.License.ProductVersionMax)
                {
                    return this.License.ProductVersionMin.ToString();
                }

                return $"{this.License.ProductVersionMin} - {this.License.ProductVersionMax}";
            }
        }

        public async Task ApplyNewLicense()
        {
            string data = await this.dialogCoordinator.ShowInputAsync(this, "Enter license data", "Paste the license data received from Lithnet in the text box below");

            if (string.IsNullOrWhiteSpace(data))
            {
                return;
            }

            try
            {
                data = data.Trim().Replace("\r", "").Replace("\n", "");

                var validationResult = this.licenseManager.ValidateLicense(data);

                if (validationResult.State == LicenseState.Licensed)
                {
                    this.LicenseData = data;
                    this.ValidationResult = validationResult;
                }
                else
                {
                    await this.dialogCoordinator.ShowMessageAsync(this, "Unable to apply license", validationResult.Message);
                }

                this.licenseDataProvider.LicenseDataChanged();
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Unable to apply new license");
                await this.dialogCoordinator.ShowMessageAsync(this, "Unable to apply license", ex.Message);
            }
        }

        public void HelpLinkRenewNow()
        {

        }

        public void HelpLinkEnterpriseEditionLearnMore()
        {

        }

        public void HelpLinkGetLicenseHelp()
        {

        }

        public bool CanRemoveLicense => this.License?.Type != LicenseType.BuiltIn;

        public async Task RemoveLicense()
        {
            try
            {
                var result = await this.dialogCoordinator.ShowMessageAsync(this, "Delete license data", "By removing the license data, the application will revert immediately to standard edition, and any enterprise edition features will no longer be available. Are you sure you want to delete the existing license data?", MessageDialogStyle.AffirmativeAndNegative);

                if (result == MessageDialogResult.Affirmative)
                {
                    this.LicenseData = null;
                    this.ValidationResult = this.licenseManager.ValidateLicense(null);
                    this.licenseDataProvider.LicenseDataChanged();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Unable to delete license data");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Unable to delete license data\r\n{ex.Message}");
            }
        }

        private static readonly SortedList<double, Func<TimeSpan, string>> offsets =
            new SortedList<double, Func<TimeSpan, string>>
            {
                { 0.75, _ => "less than a minute"},
                { 1.5, _ => "about a minute"},
                { 45, x => $"{x.TotalMinutes:F0} minutes"},
                { 90, x => "about an hour"},
                { 1440, x => $"about {x.TotalHours:F0} hours"},
                { 2880, x => "a day"},
                { 129600, x => $"{x.TotalDays:F0} days"},
                //{ 86400, x => "about a month"},
                { 525600, x => $"{x.TotalDays / 30:F0} months"},
                { 1051200, x => "about a year"},
                { double.MaxValue, x => $"{x.TotalDays / 365:F0} years"}
            };

        private static string ToRelativeDate(DateTime input)
        {
            TimeSpan x = DateTime.UtcNow - input;
            string Suffix = x.TotalMinutes > 0 ? " ago" : " from now";
            x = new TimeSpan(Math.Abs(x.Ticks));
            return offsets.First(n => x.TotalMinutes < n.Key).Value(x) + Suffix;
        }
    }
}
