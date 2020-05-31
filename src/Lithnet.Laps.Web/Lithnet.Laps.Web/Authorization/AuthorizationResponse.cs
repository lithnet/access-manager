using System;
using System.Collections.Generic;

namespace Lithnet.Laps.Web.Authorization
{
    public class AuthorizationResponse
    {
        /// <summary>
        /// An identifer that provides context as to the rule that was used to make the authorization decision, if one was made
        /// </summary>
        public string MatchedRuleDescription { get; set; }

        /// <summary>
        /// If the user was successfully authorized, then this TimeSpan will be used to determine the new expiry date of the LAPS password. If it is set to zero, then no alternation to the LAPS password expiry date will be made.
        /// </summary>
        public TimeSpan ExpireAfter { get; set; }

        /// <summary>
        /// A list of email addresses that should be notified of this success or failure event
        /// </summary>
        public IList<string> NotificationRecipients { get; set; }

        /// <summary>
        /// Additional information about the authorization decision that can be included in audit messages
        /// </summary>
        public string AdditionalInformation { get; set; }

        /// <summary>
        /// An identifier that provides context as to the principal (user or group) that was used to make the authorization decision, if one was made
        /// </summary>
        public string MatchedAcePrincipal { get; set; }

        /// <summary>
        /// An AuthorizationResponseCode value that indicates the status of the authorization request
        /// </summary>
        public AuthorizationResponseCode Code { get; set; }

        /// <summary>
        /// Gets a value indicating if the AuthorizationResponseCode indicates that the user is authorized to read the password
        /// </summary>
        /// <returns></returns>
        internal bool IsAuthorized()
        {
            return this.Code == AuthorizationResponseCode.Success;
        }

        /// <summary>
        /// Gets a value indicating if the AuthorizationResponseCode indicates that an explicit authorization decision was made, and that no other authorization providers should offered the opportunity to process the authorization request.
        /// </summary>
        /// <returns></returns>
        internal bool IsExplicitResult()
        {
            return this.Code == AuthorizationResponseCode.Success || this.Code == AuthorizationResponseCode.ExplicitlyDenied;
        }
    }
}