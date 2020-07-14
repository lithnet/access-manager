using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.Workers
{
    public class SearchParameters
    {
        public DateTime LastFullSync { get; set; }

        public long HighestUsn { get; internal set; }

        public string Server { get; set; }
        
        public string DnsDomain { get; set; }

        public string NetBiosDomain { get; set; }

        public long LastUsn { get; set; }
    }
}
