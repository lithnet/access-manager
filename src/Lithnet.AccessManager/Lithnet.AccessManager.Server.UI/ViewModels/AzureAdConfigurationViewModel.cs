using Lithnet.AccessManager.Api;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using MahApps.Metro.SimpleChildWindow;
using Newtonsoft.Json;
using Stylet;
using System;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.AccessManager.Enterprise;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public class AzureAdConfigurationViewModel : Screen, IHelpLink
    {
        private readonly AzureAdOptions aadOptions;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IAzureAdTenantDetailsViewModelFactory tenantFactory;
        private readonly INotifyModelChangedEventPublisher eventPublisher;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IAadGraphApiProvider graphApiProvider;
        private readonly ILogger<AzureAdConfigurationViewModel> logger;
        private readonly IAmsLicenseManager licenseManager;

        public PackIconFontAwesomeKind Icon => PackIconFontAwesomeKind.DirectionsSolid;

        public AzureAdConfigurationViewModel(AzureAdOptions aadOptions, IDialogCoordinator dialogCoordinator, IAzureAdTenantDetailsViewModelFactory tenantFactory, INotifyModelChangedEventPublisher eventPublisher, IShellExecuteProvider shellExecuteProvider, IAadGraphApiProvider graphApiProvider, ILogger<AzureAdConfigurationViewModel> logger, IAmsLicenseManager licenseManager)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.graphApiProvider = graphApiProvider;
            this.logger = logger;
            this.licenseManager = licenseManager;
            this.dialogCoordinator = dialogCoordinator;
            this.aadOptions = aadOptions;
            this.tenantFactory = tenantFactory;
            this.eventPublisher = eventPublisher;

            this.licenseManager.OnLicenseDataChanged += delegate
            {
                this.NotifyOfPropertyChange(nameof(this.IsEnterpriseEdition));
                this.NotifyOfPropertyChange(nameof(this.ShowEnterpriseEditionBanner));
            };

            this.DisplayName = "Azure Active Directory";
            this.Tenants = new BindableCollection<AzureAdTenantDetailsViewModel>();
        }

        protected override void OnInitialActivate()
        {
            Task.Run(() =>
            {
                foreach (var m in this.aadOptions.Tenants)
                {
                    this.Tenants.Add(tenantFactory.CreateViewModel(m));
                }

                this.eventPublisher.Register(this);
            });
        }

        public async Task LinkHaLearnMore()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.EnterpriseEditionLearnMoreLinkHa);
        }

        public bool IsEnterpriseEdition => this.licenseManager.IsEnterpriseEdition();

        public bool ShowEnterpriseEditionBanner => this.licenseManager.IsEvaluatingOrBuiltIn() || !this.licenseManager.IsEnterpriseEdition();

        [NotifyModelChangedCollection]
        public BindableCollection<AzureAdTenantDetailsViewModel> Tenants { get; }

        public AzureAdTenantDetailsViewModel SelectedTenant { get; set; }

        public async Task Add()
        {
            DialogWindow w = new DialogWindow();
            w.Title = "Add tenant";
            w.SaveButtonIsDefault = true;
            var m = new AzureAdTenantDetails();
            var vm = this.tenantFactory.CreateViewModel(m);
            w.DataContext = vm;

            while (true)
            {
                await this.GetWindow().ShowChildWindowAsync(w);

                if (w.Result != MessageDialogResult.Affirmative)
                {
                    break;
                }

                if (this.aadOptions.Tenants.Any(t => string.Equals(t.ClientId, m.ClientId, StringComparison.OrdinalIgnoreCase)))
                {
                    await this.dialogCoordinator.ShowMessageAsync(this, "Duplicate entry", "A registration entry already exists for this tenant ID. Only one registration per tenant is allowed");
                    continue;
                }

                if (!await this.ValidateAadCredentials(m))
                {
                    continue;
                }

                this.aadOptions.Tenants.Add(m);
                this.Tenants.Add(vm);
                this.graphApiProvider.AddOrUpdateClientCredentials(m.TenantId, m.ClientId, m.ClientSecret);
                vm.TenantName = await this.graphApiProvider.GetTenantOrgName(m.TenantId);
                break;
            }
        }

        private async Task<bool> ValidateAadCredentials(AzureAdTenantDetails m)
        {
            try
            {
                await this.graphApiProvider.ValidateCredentials(m.TenantId, m.ClientId, m.ClientSecret);
                return true;
            }
            catch (AadMissingPermissionException ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Missing permissions", $"The credentials were validated, but the following permissions were missing. Please assign these API permissions to the enterprise application, and ensure admin consent has been granted\r\n\r\n{string.Join("\r\n", ex.MissingPermissions)}");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Unable to validate credentials");
                await this.dialogCoordinator.ShowMessageAsync(this, "Validation failed", $"The credentials provided could not be validated\r\n{ex.Message}");
            }

            return false;
        }

        public bool CanEdit => this.SelectedTenant != null;

        public async Task Edit()
        {
            var selectedTenant = this.SelectedTenant;

            if (selectedTenant == null)
            {
                return;
            }

            DialogWindow w = new DialogWindow();
            w.Title = "Edit tenant";
            w.SaveButtonIsDefault = true;

            var m = JsonConvert.DeserializeObject<AzureAdTenantDetails>(JsonConvert.SerializeObject(selectedTenant.Model));
            var vm = this.tenantFactory.CreateViewModel(m);

            w.DataContext = vm;

            while (true)
            {
                await this.GetWindow().ShowChildWindowAsync(w);

                if (w.Result != MessageDialogResult.Affirmative)
                {
                    break;
                }

                if (this.aadOptions.Tenants.Any(t => t != selectedTenant.Model && string.Equals(t.ClientId, m.ClientId, StringComparison.OrdinalIgnoreCase)))
                {
                    await this.dialogCoordinator.ShowMessageAsync(this, "Duplicate entry", "A registration entry already exists for this tenant ID. Only one registration per tenant is allowed");
                    continue;
                }

                if (!await this.ValidateAadCredentials(m))
                {
                    continue;
                }

                this.aadOptions.Tenants.Remove(selectedTenant.Model);

                int existingPosition = this.Tenants.IndexOf(selectedTenant);

                this.Tenants.Remove(selectedTenant);
                this.aadOptions.Tenants.Add(m);
                this.Tenants.Insert(Math.Min(existingPosition, this.Tenants.Count), vm);
                this.SelectedTenant = vm;

                this.graphApiProvider.AddOrUpdateClientCredentials(m.TenantId, m.ClientId, m.ClientSecret);
                vm.TenantName = await this.graphApiProvider.GetTenantOrgName(m.TenantId);
                break;
            }
        }

        public bool CanDelete => this.SelectedTenant != null;

        public async Task Delete()
        {
            var selectedTenant = this.SelectedTenant;

            if (selectedTenant == null)
            {
                return;
            }

            MetroDialogSettings s = new MetroDialogSettings
            {
                AnimateShow = false,
                AnimateHide = false
            };

            if (await this.dialogCoordinator.ShowMessageAsync(this, "Confirm", "Are you sure you want to delete this tenant?", MessageDialogStyle.AffirmativeAndNegative, s) == MessageDialogResult.Affirmative)
            {
                var deleting = selectedTenant;
                this.aadOptions.Tenants.Remove(deleting.Model);
                this.Tenants.Remove(deleting);
                this.SelectedTenant = this.Tenants.FirstOrDefault();
            }
        }

        public bool CanTest => this.SelectedTenant != null;

        public async Task Test()
        {
            var selectedTenant = this.SelectedTenant;

            if (selectedTenant == null)
            {
                return;
            }

            if (await this.ValidateAadCredentials(selectedTenant.Model))
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Connection successful", "Successfully authenticated to the Azure Active Directory");
            }
        }

        [NotifyModelChangedProperty]
        public int HasBeenChanged { get; set; }

        public string HelpLink => Constants.HelpLinkPageJitAccess;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
