using System.Threading.Tasks;
using Lithnet.AccessManager.Server.UI.AuthorizationRuleImport;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class ImportWizardImportTypeViewModel : Screen, IHelpLink
    {
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly ILogger logger;

        public ImportWizardImportTypeViewModel(ILogger<ImportWizardImportTypeViewModel> logger, IShellExecuteProvider shellExecuteProvider)
        {
            this.logger = logger;
            this.shellExecuteProvider = shellExecuteProvider;
        }

        public ImportMode ImportType { get; set; } = ImportMode.Laps;

        public bool ImportTypeLaps
        {
            get => this.ImportType == ImportMode.Laps;
            set
            {
                if (value)
                {
                    this.ImportType = ImportMode.Laps;
                }
            }
        }

        public bool ImportTypeBitLocker
        {
            get => this.ImportType == ImportMode.BitLocker;
            set
            {
                if (value)
                {
                    this.ImportType = ImportMode.BitLocker;
                }
            }
        }

        public bool ImportTypeLocalAdmins
        {
            get => this.ImportType == ImportMode.Rpc;
            set
            {
                if (value)
                {
                    this.ImportType = ImportMode.Rpc;
                }
            }
        }

        public bool ImportTypeFile
        {
            get => this.ImportType == ImportMode.CsvFile;
            set
            {
                if (value)
                {
                    this.ImportType = ImportMode.CsvFile;
                }
            }
        }

        public bool ImportTypeLapsWeb
        {
            get => this.ImportType == ImportMode.LapsWeb;
            set
            {
                if (value)
                {
                    this.ImportType = ImportMode.LapsWeb;
                }
            }
        }

        public async Task HelpRpcLocalAdmin()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.HelpLinkImportWizardImportTypeLocalAdminRpcHelp);
        }

        public async Task HelpCsvFileFormat()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(Constants.HelpLinkImportWizardImportTypeCsvHelp);
        }

        public string HelpLink => Constants.HelpLinkImportWizardImportType;

        public async Task Help()
        {
            if (this.HelpLink == null)
            {
                return;
            }

            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }
    }
}
