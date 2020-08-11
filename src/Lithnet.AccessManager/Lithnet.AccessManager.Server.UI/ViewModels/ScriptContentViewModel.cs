using System;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ScriptContentViewModel : PropertyChangedBase
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

        public async Task Save()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "ps1",
                OverwritePrompt = true,
                Filter = "PowerShell script (*.ps1)|*.ps1",
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
    }
}
