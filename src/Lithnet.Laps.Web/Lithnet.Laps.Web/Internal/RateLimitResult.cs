using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lithnet.Laps.Web.Internal
{
    public class RateLimitResult
    {
        public bool IsRateLimitExceeded { get; set; }

        public bool IsUserRateLimit { get; set; }

        public string IPAddress { get; set; }

        public string UserID { get; set; }

        public int Duration { get; set; }

        public int Threshold { get; set; }
    }
}