using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class JitGroupMappingViewModel : ValidatingModelBase, IViewAware
    {
        private readonly IDirectory directory;

        private readonly ILogger<JitGroupMappingViewModel> logger;

        private readonly IDialogCoordinator dialogCoordinator;

        public JitGroupMapping Model { get; }

        public UIElement View { get; set; }

        public JitGroupMappingViewModel(JitGroupMapping model, IDirectory directory, ILogger<JitGroupMappingViewModel> logger, IDialogCoordinator dialogCoordinator, IModelValidator<JitGroupMappingViewModel> validator)
        {
            this.directory = directory;
            this.logger = logger;
            this.dialogCoordinator = dialogCoordinator;
            this.Model = model;
            this.Validator = validator;
        }

        public string ComputerOU
        {
            get => this.Model.ComputerOU;
            set => this.Model.ComputerOU = value;
        }

        public string GroupOU
        {
            get => this.Model.GroupOU;
            set => this.Model.GroupOU = value;
        }

        public string GroupNameTemplate
        {
            get => this.Model.GroupNameTemplate;
            set => this.Model.GroupNameTemplate = value;
        }

        public bool EnableJitGroupDeletion
        {
            get => this.Model.EnableJitGroupDeletion;
            set => this.Model.EnableJitGroupDeletion = value;
        }

        public bool Subtree
        {
            get => this.Model.Subtree;
            set => this.Model.Subtree = value;
        }

        public bool OneLevel
        {
            get => !this.Model.Subtree;
            set => this.Model.Subtree = !value;
        }

        public GroupType GroupType
        {
            get => this.Model.GroupType;
            set => this.Model.GroupType = value;
        }

        public IEnumerable<GroupType> GroupTypeValues => Enum.GetValues(typeof(GroupType)).Cast<GroupType>();

        public async Task SelectComputerOU()
        {
            try
            {
                string basePath = this.directory.GetFullyQualifiedDomainControllerAdsPath(this.ComputerOU);
                string initialPath = this.directory.GetFullyQualifiedAdsPath(this.ComputerOU);

                var container = NativeMethods.ShowContainerDialog(this.GetHandle(), "Select OU", "Select computer OU", basePath, initialPath);
                if (container != null)
                {
                    this.ComputerOU = container;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Select OU error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
            }
        }

        public async Task SelectGroupOU()
        {
            try
            {
                string basePath = this.directory.GetFullyQualifiedDomainControllerAdsPath(this.ComputerOU);
                string initialPath = this.directory.GetFullyQualifiedAdsPath(this.ComputerOU);

                var container = NativeMethods.ShowContainerDialog(this.GetHandle(), "Select OU", "Select group OU", basePath, initialPath);
                if (container != null)
                {
                    this.GroupOU = container;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Select OU error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"An error occurred when processing the request\r\n{ex.Message}");
            }
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }
    }
}
