using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server
{
    public class RoleFulfillmentResult
    {
        public bool IsSuccess { get; set; }

        public RoleFulfillmentError Error { get; set; }

        public DateTime Expiry { get; set; }

        public string RoleName { get; set; }

    }

    public enum RoleFulfillmentError
    {
        None= 0,
        NotAuthorized = 1,
        FulfillmentError = 2,
        RateLimitExceeded = 3,
    }
}
