using System;
using System.Collections.Generic;

namespace Lithnet.Laps.Web.Authorization
{
    public abstract class AuthorizationResponse
    {
        /// <summary>
        /// An identifier that provides context as to the rule that was used to make the authorization decision, if one was made
        /// </summary>
        public string MatchedRuleDescription { get; set; }

        /// <summary>
        /// A list of email addresses that should be notified of this success or failure event
        /// </summary>
        public IList<string> NotificationChannels { get; set; }

        /// <summary>
        /// Additional information about the authorization decision that can be included in audit messages
        /// </summary>
        public string AdditionalInformation { get; set; }

        /// <summary>
        /// An identifier that provides context as to the principal (user or group) that was used to make the authorization decision, if one was made
        /// </summary>
        public string Trustee { get; set; }

        /// <summary>
        /// The access type that was approved or denied in this response
        /// </summary>
        internal abstract AccessMask EvaluatedAccess { get; }

        /// <summary>
        /// An AuthorizationResponseCode value that indicates the status of the authorization request
        /// </summary>
        public AuthorizationResponseCode Code { get; set; }

        internal AuthorizationResponse()
        {
        }

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

        internal static AuthorizationResponse CreateAuthorizationResponse (AccessMask mask)
        {
            if (mask == AccessMask.Laps)
            {
                return new LapsAuthorizationResponse();
            }

            if (mask == AccessMask.Jit)
            {
                return new JitAuthorizationResponse();
            }

            throw new ArgumentException($"Invalid value for mask: {mask}");
        }
    }
}