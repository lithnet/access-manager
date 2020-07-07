using System;
using System.Collections.Generic;
using System.Linq;
using Lithnet.AccessManager.Configuration;
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