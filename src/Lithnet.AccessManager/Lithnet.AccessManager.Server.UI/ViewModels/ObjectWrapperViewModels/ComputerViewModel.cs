using Stylet;
using System;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Providers;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class ComputerViewModel : Screen
    {
        private readonly IAuthorityDataProvider authorityDataProvider;
        private readonly ILogger<ComputerViewModel> logger;
        public Task Initialization { get; }

        public IComputer Model { get; }

        public ComputerViewModel(IComputer model, IAuthorityDataProvider authorityDataProvider, ILogger<ComputerViewModel> logger)
        {
            this.Model = model;
            this.authorityDataProvider = authorityDataProvider;
            this.logger = logger;
            this.DisplayName = this.Model.DisplayName;
            this.Initialization = this.Initialize();
        }

        private async Task Initialize()
        {
            try
            {
                this.IsLoading = true;
                this.AuthorityName = await this.authorityDataProvider.GetAuthorityName(this.Model);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Could not initialize the view model");
                this.ErrorMessageText = ex.ToString();
                this.ErrorMessageHeaderText = "An initialization error occurred";
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        public string ErrorMessageText { get; set; }

        public string ErrorMessageHeaderText { get; set; }

        public bool IsLoading { get; set; }

        public string AuthorityDeviceId => this.Model.AuthorityDeviceId;

        public string AuthorityId => this.Model.AuthorityId;

        public AuthorityType AuthorityType => this.Model.AuthorityType;

        public string AuthorityName { get; private set; }

        public string ComputerName => this.Model.Name;

        public string Description => this.Model.Description;

        public string DnsHostName => this.Model.DnsHostName;

        public string FullyQualifiedName => this.Model.FullyQualifiedName;

        public string ObjectID => this.Model.ObjectID;
    }
}
