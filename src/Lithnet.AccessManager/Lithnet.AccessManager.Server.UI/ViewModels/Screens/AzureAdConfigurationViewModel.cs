using Lithnet.AccessManager.Api;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using MahApps.Metro.SimpleChildWindow;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Stylet;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public class AzureAdConfigurationViewModel : Conductor<PropertyChangedBase>.Collection.OneActive, IHelpLink
    {
        private readonly AzureAdOptions aadOptions;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IViewModelFactory<AzureAdTenantDetailsViewModel, AzureAdTenantDetails> tenantFactory;
        private readonly INotifyModelChangedEventPublisher eventPublisher;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IAadGraphApiProvider graphApiProvider;
        private readonly ILogger<AzureAdConfigurationViewModel> logger;
        private readonly ApiAuthenticationOptions agentOptions;

        public object Icon => PackIconMaterialKind.Triangle;

        public AzureAdConfigurationViewModel(AzureAdLithnetLapsConfigurationViewModel lithnetLapsVm, AzureAdOptions aadOptions, IDialogCoordinator dialogCoordinator, IViewModelFactory<AzureAdTenantDetailsViewModel, AzureAdTenantDetails> tenantFactory, INotifyModelChangedEventPublisher eventPublisher, IShellExecuteProvider shellExecuteProvider, IAadGraphApiProvider graphApiProvider, ILogger<AzureAdConfigurationViewModel> logger, ApiAuthenticationOptions agentOptions, IViewModelFactory<EnterpriseEditionBannerViewModel, EnterpriseEditionBannerModel> enterpriseEditionViewModelFactory)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.graphApiProvider = graphApiProvider;
            this.logger = logger;
            this.agentOptions = agentOptions;
            this.dialogCoordinator = dialogCoordinator;
            this.aadOptions = aadOptions;
            this.tenantFactory = tenantFactory;
            this.eventPublisher = eventPublisher;

            this.DisplayName = "Azure Active Directory";

            //this.Items.Add(lithnetLapsVm);

            this.Tenants = new BindableCollection<AzureAdTenantDetailsViewModel>();

            this.EnterpriseEdition = enterpriseEditionViewModelFactory.CreateViewModel(new EnterpriseEditionBannerModel
            {
                RequiredFeature = Enterprise.LicensedFeatures.AzureAdDeviceSupport,
                Link = Constants.EnterpriseEditionLearnMoreLinkAzureAdDevices
            });
        }

        public EnterpriseEditionBannerViewModel EnterpriseEdition { get; set; }


        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public bool AllowAzureAdJoinedDevices
        {
            get => this.agentOptions.AllowAzureAdJoinedDeviceAuth;
            set => this.agentOptions.AllowAzureAdJoinedDeviceAuth = value;
        }

        [NotifyModelChangedProperty(RequiresServiceRestart = true)]
        public bool AllowAzureAdRegisteredDevices
        {
            get => this.agentOptions.AllowAzureAdRegisteredDeviceAuth;
            set => this.agentOptions.AllowAzureAdRegisteredDeviceAuth = value;
        }


        protected override void OnInitialActivate()
        {
            Task.Run(this.Initialize);
        }

        private void Initialize()
        {
            try
            {
                foreach (var m in this.aadOptions.Tenants)
                {
                    this.Tenants.Add(tenantFactory.CreateViewModel(m));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not initialize the view model");
                this.ErrorMessageText = ex.ToString();
                this.ErrorMessageHeaderText = "An initialization error occurred";
            }
            finally
            {
                this.eventPublisher.Register(this);
            }
        }

        public string ErrorMessageText { get; set; }

        public string ErrorMessageHeaderText { get; set; }

        [NotifyModelChangedCollection]
        public BindableCollection<AzureAdTenantDetailsViewModel> Tenants { get; }

        public AzureAdTenantDetailsViewModel SelectedTenant { get; set; }

        public async Task Add()
        {
            try
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
                    vm.TenantName = await this.graphApiProvider.GetTenantOrgName(m.TenantId, true);
                    break;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
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
            try
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
                    vm.TenantName = await this.graphApiProvider.GetTenantOrgName(m.TenantId, true);
                    break;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanDelete => this.SelectedTenant != null;

        public async Task Delete()
        {
            try
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
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
            }
        }

        public bool CanTest => this.SelectedTenant != null;

        public async Task Test()
        {
            try
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
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Could not complete the operation");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not complete the operation\r\n{ex.Message}");
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
