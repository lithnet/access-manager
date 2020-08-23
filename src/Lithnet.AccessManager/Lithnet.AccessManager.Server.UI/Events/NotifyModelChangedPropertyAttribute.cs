using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.UI
{
    public class NotifyModelChangedPropertyAttribute : Attribute
    {
        public bool RequiresServiceRestart { get; set; }
    }
}
