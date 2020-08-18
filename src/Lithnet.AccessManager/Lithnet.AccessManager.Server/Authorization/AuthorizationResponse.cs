using System;
using System.Collections.Generic;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.Extensions;

namespace Lithnet.AccessManager.Server.Authorization
{
    public abstract class AuthorizationResponse
    {
        /// <summary>
        /// An identifier that provides context as to the rule that was used to make the authorization decision, if one was made
        /// </summary>
        public string MatchedRuleDescription { get; set; }

        /// <summary>
        /// The ID of the rule that was used to make this authorization decision, if one was made
        /// </summary>
        public string MatchedRule { get; set; }

        /// <summary>
        /// A list of email addresses that should be notified of this success or failure event
        /// </summary>
        public IList<string> NotificationChannels { get; set; }

        /// <summary>
        /// The access type that was approved or denied in this response
        /// </summary>
        public abstract AccessMask EvaluatedAccess { get; }

        /// <summary>
        /// An AuthorizationResponseCode value that indicates the status of the authorization request
        /// </summary>
        public AuthorizationResponseCode Code { get; set; }

        protected AuthorizationResponse()
        {
        }

        /// <summary>
        /// Gets a value indicating if the AuthorizationResponseCode indicates that the user is authorized to read the password
        /// </summary>
        /// <returns></returns>
        public bool IsAuthorized()
        {
            return this.Code == AuthorizationResponseCode.Success;
        }

        /// <summary>
        /// Gets a value indicating if the AuthorizationResponseCode indicates that an explicit authorization decision was made, and that no other authorization providers should offered the opportunity to process the authorization request.
        /// </summary>
        /// <returns></returns>
        public bool IsExplicitResult()
        {
            return this.Code == AuthorizationResponseCode.Success || this.Code == AuthorizationResponseCode.ExplicitlyDenied;
        }

        public static AuthorizationResponse CreateAuthorizationResponse(AccessMask mask)
        {
            mask.ValidateAccessMask();

            if (mask == AccessMask.LocalAdminPassword)
            {
                return new LapsAuthorizationResponse();
            }

            if (mask == AccessMask.Jit)
            {
                return new JitAuthorizationResponse();
            }

            if (mask == AccessMask.LocalAdminPasswordHistory)
            {
                return new LapsHistoryAuthorizationResponse();
            }

            if (mask == AccessMask.BitLocker)
            {
                return new BitLockerAuthorizationResponse();
            }

            throw new ArgumentException($"Invalid value for mask: {mask}");
        }
    }
}