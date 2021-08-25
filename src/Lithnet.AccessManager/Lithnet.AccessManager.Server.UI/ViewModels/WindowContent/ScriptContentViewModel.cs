using System;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ScriptContentViewModel : Screen, IExternalDialogAware, IHasSize
    {
        private readonly IDialogCoordinator dialogCoordinator;

        public ScriptContentViewModel(IDialogCoordinator dialogCoordinator)
        {
            this.dialogCoordinator = dialogCoordinator;
        }

        public string ScriptText { get; set; }

        public string HelpText { get; set; }

        public void Copy()
        {
            Clipboard.SetText(this.ScriptText);
        }

        public string DefaultExt { get; set; } = "ps1";

        public string Filter { get; set; } = "PowerShell script (*.ps1)|*.ps1";

        public async Task Save()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = this.DefaultExt,
                OverwritePrompt = true,
                Filter = this.Filter,
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                await System.IO.File.WriteAllTextAsync(dialog.FileName, this.ScriptText);
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not create the file\r\n{ex.Message}");
                return;
            }

        }

        public bool CancelButtonVisible { get; } = true;
        
        public bool SaveButtonVisible { get; } = false;
        
        public bool CancelButtonIsDefault { get; } = true;
        
        public bool SaveButtonIsDefault { get; } = false;
        
        public string SaveButtonName { get; } = null;
        
        public string CancelButtonName { get; } = "Close";
        
        public int Width { get; } = 800;

        public int Height { get; } = 600;
    }
}
