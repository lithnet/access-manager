namespace Lithnet.AccessManager.Web.Authorization
{
    public enum AuthorizationResponseCode
    {
        /// <summary>
        /// No authorization state is provided. This implicitly denies access to the user.
        /// 
        /// If an authorization provider provides this response code, then subsequent authorization providers will be offered a chance to authorize the user
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// The user is allowed to access the password for the specified computer
        /// 
        /// If an authorization provider provides this response code, processing stops, and subsequent authorization providers are not called
        /// </summary>
        Success = 1,

        /// <summary>
        /// There were no rules found that apply to the specific computer. This implicitly denies access to a user.
        /// 
        /// If an authorization provider provides this response code, then subsequent authorization providers will be offered a chance to authorize the user
        /// </summary>
        NoMatchingRuleForComputer = 2,

        /// <summary>
        /// There were no rules found that apply to the specific user. This implicitly denies access to the user.
        /// 
        /// If an authorization provider provides this response code, then subsequent authorization providers will be offered a chance to authorize the user
        /// </summary>
        NoMatchingRuleForUser = 4,

        /// <summary>
        /// The users is explicitly prohibited from accessing the password for the computer. This is usually the response from a 'deny' ACL. 
        /// 
        /// If an authorization provider provides this response code, processing stops, and subsequent authorization providers are not called
        /// </summary>
        ExplicitlyDenied = 8,
    }
}