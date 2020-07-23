using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryForestConfigurationViewModel : PropertyChangedBase, IViewAware
    {
        private readonly IDirectory directory;

        private readonly IActiveDirectoryDomainConfigurationViewModelFactory domainFactory;

        private readonly IDialogCoordinator dialogCoordinator;

        private readonly ILogger<ActiveDirectoryForestConfigurationViewModel> logger;

        private List<ActiveDirectoryDomainConfigurationViewModel> domains;

        public ActiveDirectoryForestConfigurationViewModel(Forest forest, IDialogCoordinator dialogCoordinator, IActiveDirectoryDomainConfigurationViewModelFactory domainFactory, IDirectory directory, ILogger<ActiveDirectoryForestConfigurationViewModel> logger)
        {
            this.directory = directory;
            this.domainFactory = domainFactory;
            this.dialogCoordinator = dialogCoordinator;
            this.Forest = forest;
            this.logger = logger;

            this.RefreshSchemaStatus();
        }

        public void RefreshSchemaStatus()
        {
            _ = this.PopulateLithnetSchemaStatus();
            _ = this.PopulateMsLapsSchemaStatus();
        }

        public string MsLapsSchemaPresentText { get; set; }

        public string LithnetAccessManagerSchemaPresentText { get; set; }

        public Forest Forest { get; }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public UIElement View { get; set; }

        public List<ActiveDirectoryDomainConfigurationViewModel> Domains
        {
            get
            {
                if (this.domains == null)
                {
                    this.domains = new List<ActiveDirectoryDomainConfigurationViewModel>();

                    foreach (var domain in Forest.Domains.OfType<Domain>())
                    {
                        this.Domains.Add(domainFactory.CreateViewModel(domain));
                    }

                    this.SelectedDomain = this.Domains.FirstOrDefault();
                }

                return this.domains;
            }
        }

        public ActiveDirectoryDomainConfigurationViewModel SelectedDomain { get; set; }

        public string Name => this.Forest.Name;

        public string ForestFunctionalLevel
        {
            get
            {
                return this.Forest.ForestModeLevel switch
                {
                    0 => "Windows 2000 Server",
                    1 => "Windows Server 2003 Mixed Mode",
                    2 => "Windows Server 2003",
                    3 => "Windows Server 2008",
                    4 => "Windows Server 2008 R2",
                    5 => "Windows Server 2012",
                    6 => "Windows Server 2012 R2",
                    var e when e >= 7 => "Windows Server 2016 or higher",
                    _ => "Unknown forest functional level"
                };
            }
        }

        private bool Is2016FunctionalLevel()
        {
            return this.Forest.ForestModeLevel >= 7;
        }

        public bool CanExtendSchemaLithnetAccessManager => this.IsNotLithnetSchemaPresent;

        public async Task ExtendSchemaLithnetAccessManager()
        {
            var vm = new ScriptContentViewModel(this.dialogCoordinator)
            {
                HelpText = "Run the following script as an account that is a member of the 'Schema Admins' group",
                ScriptText = ScriptTemplates.UpdateAdSchemaTemplate
            };

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                DataContext = vm,
                SaveButtonVisible = false,
                CancelButtonName = "Close"
            };

            w.ShowDialog();

            await this.PopulateLithnetSchemaStatus();
        }

        public bool IsLithnetSchemaPresent { get; set; }

        public bool IsNotLithnetSchemaPresent => !this.IsLithnetSchemaPresent;

        public bool IsMsLapsSchemaPresent { get; set; }

        public bool IsNotMsLapsSchemaPresent => !this.IsMsLapsSchemaPresent;

        private async Task PopulateLithnetSchemaStatus()
        {
            await Task.Run(() =>
            {
                try
                {
                    var schema = ActiveDirectorySchema.GetSchema(new DirectoryContext(DirectoryContextType.Forest, this.Forest.Name));
                    schema.FindProperty("lithnetAdminPassword");
                    this.IsLithnetSchemaPresent = true;
                    this.LithnetAccessManagerSchemaPresentText = "Present";
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    this.IsLithnetSchemaPresent = false;
                    this.LithnetAccessManagerSchemaPresentText = "Not present";
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Could not determine Lithnet Access Manager schema status");
                    this.IsLithnetSchemaPresent = false;
                    this.LithnetAccessManagerSchemaPresentText = "Error looking up schema";
                }
            }).ConfigureAwait(false);
        }

        private async Task PopulateMsLapsSchemaStatus()
        {
            await Task.Run(() =>
            {
                try
                {
                    var schema = ActiveDirectorySchema.GetSchema(new DirectoryContext(DirectoryContextType.Forest, this.Forest.Name));
                    schema.FindProperty("ms-Mcs-AdmPwd");
                    this.IsMsLapsSchemaPresent = true;
                    this.MsLapsSchemaPresentText = "Present";
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    this.IsMsLapsSchemaPresent = false;
                    this.MsLapsSchemaPresentText = "Not present";
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Could not determine Microsoft LAPS schema status");
                    this.IsMsLapsSchemaPresent = false;
                    this.MsLapsSchemaPresentText = "Error looking up schema";
                }
            }).ConfigureAwait(false);
        }
    }
}
