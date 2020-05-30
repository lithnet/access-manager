using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.JsonTargets
{
    public class AuthorizationResponse
    {
        public string MatchedRuleDescription { get; set; }

        public TimeSpan ExpireAfter { get; set; }

        public IList<string> NotificationRecipients { get; set; }

        public string AdditionalInformation { get; set; }

        public string MatchedAcePrincipal { get; set; }

        public AuthorizationResponseCode ResponseCode { get; set; }

        public bool IsAuthorized()
        {
            return this.ResponseCode == AuthorizationResponseCode.Success;
        }
    }
}