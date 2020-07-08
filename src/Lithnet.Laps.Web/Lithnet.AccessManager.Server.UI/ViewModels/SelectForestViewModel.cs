using System.Collections.Generic;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SelectForestViewModel : PropertyChangedBase
    {
        public SelectForestViewModel()
        {
        }

        public string SelectedForest { get; set; }

        public List<string> AvailableForests { get; set; }
    }
}