using System.Collections.Generic;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SelectForestViewModel : Screen, IExternalDialogAware
    {
        public SelectForestViewModel()
        {
            this.DisplayName = "Select forest";
        }

        public string SelectedForest { get; set; }

        public List<string> AvailableForests { get; } = new List<string>();

        public bool CancelButtonVisible { get; set; } = true;

        public bool SaveButtonVisible { get; set; } = true;

        public bool CancelButtonIsDefault { get; set; } = false;

        public bool SaveButtonIsDefault { get; set; } = true;

        public string SaveButtonName { get; set; } = "Next...";

        public string CancelButtonName { get; set; } = "Cancel";
    }
}