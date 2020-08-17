namespace Lithnet.AccessManager.Service
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