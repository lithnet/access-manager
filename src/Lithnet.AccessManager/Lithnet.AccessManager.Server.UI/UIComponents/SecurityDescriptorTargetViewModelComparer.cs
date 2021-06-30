using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Lithnet.AccessManager.Server.UI
{
    internal class SecurityDescriptorTargetViewModelComparer : IComparer<SecurityDescriptorTargetViewModel>, IComparer
    {
        public ListSortDirection SortDirection { get; set; } = ListSortDirection.Descending;

        public int Compare(SecurityDescriptorTargetViewModel x, SecurityDescriptorTargetViewModel y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            if (x.Type != y.Type)
            {
                return this.SortDirection == ListSortDirection.Descending ? (int) x.Type - (int) y.Type : (int) y.Type - (int) x.Type;
            }

            if (x.Type != Configuration.TargetType.AdContainer)
            {
                return this.SortDirection == ListSortDirection.Descending ? string.Compare(y.DisplayName, x.DisplayName, StringComparison.OrdinalIgnoreCase) : string.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);
            }

            var xparts = x.DisplayName.Split(',').Reverse().ToList();
            var yparts = y.DisplayName.Split(',').Reverse().ToList();

            int maxParts = Math.Min(xparts.Count, yparts.Count);
            for (int i = 0; i < maxParts ; i++)
            {
                int val = string.Compare(xparts[i], yparts[i], StringComparison.OrdinalIgnoreCase);
                if (val != 0)
                {
                    if (this.SortDirection == ListSortDirection.Ascending)
                    {
                        return val;
                    }
                    else
                    {
                        return val * -1;
                    }
                }
            }

            string xr = string.Concat(xparts);
            string yr = string.Concat(yparts);

            return this.SortDirection == ListSortDirection.Descending ? string.Compare(yr, xr, StringComparison.OrdinalIgnoreCase) : string.Compare(xr, yr, StringComparison.OrdinalIgnoreCase);
        }

        public int Compare(object x, object y)
        {
            return this.Compare(x as SecurityDescriptorTargetViewModel, y as SecurityDescriptorTargetViewModel);
        }
    }
}