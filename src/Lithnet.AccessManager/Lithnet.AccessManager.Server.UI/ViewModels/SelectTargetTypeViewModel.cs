using System;
using System.Collections.Generic;
using System.Linq;
using Lithnet.AccessManager.Api;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Providers;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SelectTargetTypeViewModel : PropertyChangedBase
    {
        private readonly AzureAdOptions azureAdOptions;
        private readonly ApiAuthenticationOptions apiOptions;

        public SelectTargetTypeViewModel(AzureAdOptions azureAdOptions, ApiAuthenticationOptions apiOptions, IDomainTrustProvider domainTrustProvider, IAzureAdTenantDetailsViewModelFactory tenantDetailsFactory)
        {
            this.azureAdOptions = azureAdOptions;
            this.apiOptions = apiOptions;

            this.AvailableForests = domainTrustProvider.GetForests().Select(t => t.Name).ToList();
            this.AvailableAads = new List<AzureAdTenantDetailsViewModel>();

            foreach (var tenant in azureAdOptions.Tenants)
            {
                this.AvailableAads.Add(tenantDetailsFactory.CreateViewModel(tenant));
            }

            this.SelectedForest = this.AvailableForests.FirstOrDefault();
            this.SelectedAad = this.AvailableAads.FirstOrDefault();
        }

        public string SelectedForest { get; set; }

        public List<string> AvailableForests { get; set; }

        public TargetType TargetType { get; set; }

        public bool ShowForest => this.TargetType == TargetType.AdComputer || this.TargetType == TargetType.AdGroup;

        public IEnumerable<TargetType> TargetTypeValues
        {
            get
            {
                yield return TargetType.AdComputer;
                yield return TargetType.AdGroup;
                yield return TargetType.AdContainer;

                if (azureAdOptions.Tenants.Count > 0)
                {
                    yield return TargetType.AadComputer;
                    yield return TargetType.AadGroup;
                }

                if (apiOptions.AllowAmsManagedDeviceAuth)
                {
                    yield return TargetType.AmsComputer;
                    yield return TargetType.AmsGroup;
                }
            }
        }

        public AzureAdTenantDetailsViewModel SelectedAad { get; set; }

        public bool ShowAad => this.TargetType == TargetType.AadComputer || this.TargetType == TargetType.AadGroup;

        public List<AzureAdTenantDetailsViewModel> AvailableAads { get; set; }
    }
}