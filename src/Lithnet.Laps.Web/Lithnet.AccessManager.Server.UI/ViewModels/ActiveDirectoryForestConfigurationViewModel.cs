using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryForestConfigurationViewModel : PropertyChangedBase, IViewAware
    {
        private readonly IDirectory directory;

        private readonly IActiveDirectoryDomainConfigurationViewModelFactory domainFactory;

        private readonly IDialogCoordinator dialogCoordinator;

        private List<ActiveDirectoryDomainConfigurationViewModel> domains;

        public ActiveDirectoryForestConfigurationViewModel(Forest forest, IDialogCoordinator dialogCoordinator, IActiveDirectoryDomainConfigurationViewModelFactory domainFactory, IDirectory directory)
        {
            this.directory = directory;
            this.domainFactory = domainFactory;
            this.dialogCoordinator = dialogCoordinator;
            this.Forest = forest;
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
                    var e when e >= 7 => "Windows Server 2016",
                    _ => "Unknown forest functional level"
                };
            }
        }

        private bool Is2016FunctionalLevel()
        {
            return this.Forest.ForestModeLevel >= 7;
        }

        public bool CanExtendSchemaMsLaps { get; set; }

        public void ExtendSchemaMsLaps()
        {

        }

        public bool CanExtendSchemaLithnetAccessManager { get; set; }

        public void ExtendSchemaLithnetAccessManager()
        {

        }

        private void PopulateLithnetSchemaStatus()
        {
            try
            {
                var schema = ActiveDirectorySchema.GetSchema(new DirectoryContext(DirectoryContextType.Forest, this.Forest.Name));
                schema.FindProperty("lithnetEncryptedAdminPassword");
                this.CanExtendSchemaLithnetAccessManager = false;
                this.LithnetAccessManagerSchemaPresentText = "Present";
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                this.CanExtendSchemaLithnetAccessManager = true;
                this.LithnetAccessManagerSchemaPresentText = "Not present";
            }
            catch
            {
                this.CanExtendSchemaLithnetAccessManager = false;
                this.LithnetAccessManagerSchemaPresentText = "Error looking up schema";
            }
        }

        private void PopulateMsLapsSchemaStatus()
        {
            try
            {
                var schema = ActiveDirectorySchema.GetSchema(new DirectoryContext(DirectoryContextType.Forest, this.Forest.Name));
                schema.FindProperty("ms-Mcs-AdmPwd");
                this.CanExtendSchemaMsLaps = false;
                this.MsLapsSchemaPresentText = "Present";
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                this.CanExtendSchemaMsLaps = true;
                this.MsLapsSchemaPresentText = "Not present";
            }
            catch
            {
                this.CanExtendSchemaMsLaps = false;
                this.MsLapsSchemaPresentText = "Error looking up schema";
            }
        }
    }
}
