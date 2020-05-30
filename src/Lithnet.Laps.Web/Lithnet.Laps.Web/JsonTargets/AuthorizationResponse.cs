using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lithnet.Laps.Web.JsonTargets
{
    public class AuthorizationResponse
    {
        public string MatchedAceID { get; set; }

        public TimeSpan ExpireAfter { get; set; }

        public IList<string> NotificationRecipients { get; set; }

        public string AdditionalInformation { get; set; }

        public AuthorizationResponseCode ResponseCode { get; set; }
    }
}