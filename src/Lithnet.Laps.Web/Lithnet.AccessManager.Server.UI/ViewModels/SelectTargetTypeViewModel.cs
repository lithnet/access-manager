using System;
using System.Collections.Generic;
using System.Linq;
using Lithnet.AccessManager.Configuration;
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

        public IEnumerable<TargetType> TargetTypeValues
        {
            get
            {
                return Enum.GetValues(typeof(TargetType)).Cast<TargetType>();
            }
        }
    }
}