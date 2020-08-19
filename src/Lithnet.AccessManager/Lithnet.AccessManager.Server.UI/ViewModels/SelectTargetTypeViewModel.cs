using System;
using System.Collections.Generic;
using System.Linq;
using Lithnet.AccessManager.Server.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class SelectTargetTypeViewModel : PropertyChangedBase
    {
        public SelectTargetTypeViewModel()
        {
        }

        public string SelectedForest { get; set; }

        public List<string> AvailableForests { get; set; }

        public TargetType TargetType { get; set; }

        public bool ShowForest => this.TargetType != TargetType.Container;

        public IEnumerable<TargetType> TargetTypeValues => Enum.GetValues(typeof(TargetType)).Cast<int>().OrderByDescending(t => t).Cast<TargetType>();
    }
}