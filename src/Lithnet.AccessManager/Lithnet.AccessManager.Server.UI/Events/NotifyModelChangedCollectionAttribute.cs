using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.UI
{
    public class NotifyModelChangedCollectionAttribute : Attribute
    {
        public bool RequiresServiceRestart { get; set; }

    }
}
