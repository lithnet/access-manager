using System;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Lithnet.AccessManager.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class FileSelectionViewModel : ValidatingModelBase, IViewAware
    {
        private readonly IDialogCoordinator dialogCoordinator;

        private readonly PropertyInfo property;

        private readonly object model;

        private bool shouldValidate = true;

        public FileSelectionViewModel(object model, Expression<Func<string>> property, string basePath, IDialogCoordinator dialogCoordinator)
        {
            this.model = model;
            var expr = (MemberExpression)property.Body;
            var prop = (PropertyInfo)expr.Member;
            this.property = prop;
            this.dialogCoordinator = dialogCoordinator;
            this.BasePath = basePath;

            this.Validator = new FluentModelValidator<FileSelectionViewModel>(new FileSelectionViewModelValidator());
            this.Validate();
        }

        public string File
        {
            get
            {
                return this.property.GetValue(model) as string;
            }
            set
            {
                this.property.SetValue(model, value);
            }
        }

        public UIElement View { get; set; }

        public string DefaultFileExtension { get; set; }

        public string Filter { get; set; }

        public string BasePath { get; }

        public string NewFileContent { get; set; }

        public bool ShouldValidate
        {
            get => this.shouldValidate;
            set
            {
                this.shouldValidate = value;
                this.Validate();
            }
        }

        public bool ShowSelectFile { get; set; } = true;

        public bool CanSelectFile { get; set; } = true;

        public void SelectFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.DefaultExt = this.DefaultFileExtension;
            openFileDialog.DereferenceLinks = true;
            openFileDialog.Filter = this.Filter; ;
            openFileDialog.Multiselect = false;

            if (!string.IsNullOrWhiteSpace(this.File))
            {
                try
                {
                    string builtPath = AppPathProvider.GetFullPath(this.File, this.BasePath);
                    openFileDialog.InitialDirectory = Path.GetDirectoryName(builtPath);
                    openFileDialog.FileName = Path.GetFileName(builtPath);
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(openFileDialog.InitialDirectory))
                {
                openFileDialog.InitialDirectory = this.BasePath;
            }

            if (openFileDialog.ShowDialog(this.GetWindow()) == true)
            {
                this.File = AppPathProvider.GetRelativePath(openFileDialog.FileName, this.BasePath);
            }
        }

        public bool ShowEditFile { get; set; } = true;

        public bool CanEditFile => !string.IsNullOrEmpty(this.File);

        public async Task EditFile()
        {
            try
            {
                string builtPath = AppPathProvider.GetFullPath(this.File, this.BasePath);

                ProcessStartInfo startInfo = new ProcessStartInfo(builtPath) { Verb = "Edit", UseShellExecute = true };
                Process newProcess = new Process { StartInfo = startInfo };
                newProcess.Start();
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not start default editor\r\n{ex.Message}");
            }
        }

        public bool ShowCreateNewFile { get; set; } = true;

        public bool CanCreateNewFile { get; set; } = true;

        public async Task CreateNewFile()
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = this.DefaultFileExtension,
                OverwritePrompt = true,
                Filter = this.Filter,
                InitialDirectory = this.BasePath,
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                System.IO.File.WriteAllText(dialog.FileName, this.NewFileContent);
            }
            catch (Exception ex)
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Could not create the file\r\n{ex.Message}");
                return;
            }

            this.File = AppPathProvider.GetRelativePath(dialog.FileName, this.BasePath);

            await this.EditFile();
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public override string ToString()
        {
            return this.File;
        }
    }
}
