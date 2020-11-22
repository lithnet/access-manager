using System;
using System.Net;

namespace Lithnet.AccessManager.Server
{
    public class RateLimitResult
    {
        public bool IsRateLimitExceeded { get; set; }

        public bool IsUserRateLimit { get; set; }

        public IPAddress IPAddress { get; set; }

        public string UserID { get; set; }

        public TimeSpan Duration { get; set; }

        public int Threshold { get; set; }
    }
}