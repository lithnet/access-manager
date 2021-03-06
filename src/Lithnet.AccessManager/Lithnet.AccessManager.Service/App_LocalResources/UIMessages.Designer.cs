﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Lithnet.AccessManager.Service.App_LocalResources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class UIMessages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal UIMessages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Lithnet.AccessManager.Service.App_LocalResources.UIMessages", typeof(UIMessages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Access denied.
        /// </summary>
        public static string AccessDenied {
            get {
                return ResourceManager.GetString("AccessDenied", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BitLocker recovery password.
        /// </summary>
        public static string AccessMaskBitLocker {
            get {
                return ResourceManager.GetString("AccessMaskBitLocker", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Just-in-time access.
        /// </summary>
        public static string AccessMaskJit {
            get {
                return ResourceManager.GetString("AccessMaskJit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Local admin password.
        /// </summary>
        public static string AccessMaskLocalAdminPassword {
            get {
                return ResourceManager.GetString("AccessMaskLocalAdminPassword", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Local admin password history.
        /// </summary>
        public static string AccessMaskLocalAdminPasswordHistory {
            get {
                return ResourceManager.GetString("AccessMaskLocalAdminPasswordHistory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to None.
        /// </summary>
        public static string AccessMaskNone {
            get {
                return ResourceManager.GetString("AccessMaskNone", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An authentication error occurred.
        /// </summary>
        public static string AuthNError {
            get {
                return ResourceManager.GetString("AuthNError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An unexpected error occurred during the authorization process.
        /// </summary>
        public static string AuthZError {
            get {
                return ResourceManager.GetString("AuthZError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to grant the requested access, as one or more audit notifications could not be delivered.
        /// </summary>
        public static string AuthZFailedAuditError {
            get {
                return ResourceManager.GetString("AuthZFailedAuditError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while trying to access the BitLocker recovery passwords.
        /// </summary>
        public static string BitLockerKeyAccessError {
            get {
                return ResourceManager.GetString("BitLockerKeyAccessError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The requested computer does not have any BitLocker recovery passwords.
        /// </summary>
        public static string BitLockerKeysNotPresent {
            get {
                return ResourceManager.GetString("BitLockerKeysNotPresent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to New request.
        /// </summary>
        public static string ButtonNewRequest {
            get {
                return ResourceManager.GetString("ButtonNewRequest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Request access.
        /// </summary>
        public static string ButtonRequestAccess {
            get {
                return ResourceManager.GetString("ButtonRequestAccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to locate the requested computer.
        /// </summary>
        public static string ComputerDiscoveryError {
            get {
                return ResourceManager.GetString("ComputerDiscoveryError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Computer name.
        /// </summary>
        public static string ComputerName {
            get {
                return ResourceManager.GetString("ComputerName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There are multiple computers with that name in the directory. Try specfying the computer in DOMAIN\computername format.
        /// </summary>
        public static string ComputerNameAmbiguous {
            get {
                return ResourceManager.GetString("ComputerNameAmbiguous", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The computer name contains unsupported characters.
        /// </summary>
        public static string ComputerNameInvalid {
            get {
                return ResourceManager.GetString("ComputerNameInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Computer name is required.
        /// </summary>
        public static string ComputerNameIsRequired {
            get {
                return ResourceManager.GetString("ComputerNameIsRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The computer name is too long.
        /// </summary>
        public static string ComputerNameIsTooLong {
            get {
                return ResourceManager.GetString("ComputerNameIsTooLong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The requested computer was not found in the directory.
        /// </summary>
        public static string ComputerNotFoundInDirectory {
            get {
                return ResourceManager.GetString("ComputerNotFoundInDirectory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You do not have access to use this application. The logon attempt was denied by the authentication provider.
        /// </summary>
        public static string ExternalAuthNAccessDenied {
            get {
                return ResourceManager.GetString("ExternalAuthNAccessDenied", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your access has been granted.
        /// </summary>
        public static string HeadingAccessApproved {
            get {
                return ResourceManager.GetString("HeadingAccessApproved", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BitLocker recovery passwords.
        /// </summary>
        public static string HeadingBitLockerKeys {
            get {
                return ResourceManager.GetString("HeadingBitLockerKeys", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Just-in-time access request.
        /// </summary>
        public static string HeadingJitRequest {
            get {
                return ResourceManager.GetString("HeadingJitRequest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Password details.
        /// </summary>
        public static string HeadingPasswordDetails {
            get {
                return ResourceManager.GetString("HeadingPasswordDetails", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Request access.
        /// </summary>
        public static string HeadingRequestAccess {
            get {
                return ResourceManager.GetString("HeadingRequestAccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 403 Not authorized.
        /// </summary>
        public static string Http403Heading {
            get {
                return ResourceManager.GetString("Http403Heading", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are not authorized to access this application.
        /// </summary>
        public static string Http403Message {
            get {
                return ResourceManager.GetString("Http403Message", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your request could not be processed as your user information is not recognized.
        /// </summary>
        public static string IdentityDiscoveryError {
            get {
                return ResourceManager.GetString("IdentityDiscoveryError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The client certificate could not be validated.
        /// </summary>
        public static string InvalidCertificate {
            get {
                return ResourceManager.GetString("InvalidCertificate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Just-in-time access could not be granted because an unexpected error occurred.
        /// </summary>
        public static string JitError {
            get {
                return ResourceManager.GetString("JitError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while trying to access the local admin password.
        /// </summary>
        public static string LapsPasswordError {
            get {
                return ResourceManager.GetString("LapsPasswordError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while trying to access the local admin password history.
        /// </summary>
        public static string LapsPasswordHistoryError {
            get {
                return ResourceManager.GetString("LapsPasswordHistoryError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have been logged out. Please close all browser windows..
        /// </summary>
        public static string LoggedOut {
            get {
                return ResourceManager.GetString("LoggedOut", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Logout.
        /// </summary>
        public static string Logout {
            get {
                return ResourceManager.GetString("Logout", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The requested computer does not have a local admin password.
        /// </summary>
        public static string NoLapsPassword {
            get {
                return ResourceManager.GetString("NoLapsPassword", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The requested computer does not have any historical local admin passwords.
        /// </summary>
        public static string NoLapsPasswordHistory {
            get {
                return ResourceManager.GetString("NoLapsPasswordHistory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;not set&gt;.
        /// </summary>
        public static string NoLapsPasswordPlaceholder {
            get {
                return ResourceManager.GetString("NoLapsPasswordPlaceholder", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are not authorized to access this computer.
        /// </summary>
        public static string NotAuthorized {
            get {
                return ResourceManager.GetString("NotAuthorized", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are not authorized to access this application.
        /// </summary>
        public static string NotAuthorizedMessage {
            get {
                return ResourceManager.GetString("NotAuthorizedMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Password.
        /// </summary>
        public static string Password {
            get {
                return ResourceManager.GetString("Password", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An unexpected error occurred during the pre-authorization process.
        /// </summary>
        public static string PreAuthZError {
            get {
                return ResourceManager.GetString("PreAuthZError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have exceeded the maximum number of requests allowed in a specified period of time.
        /// </summary>
        public static string RateLimitError {
            get {
                return ResourceManager.GetString("RateLimitError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You must provide the reason for your request.
        /// </summary>
        public static string ReasonRequired {
            get {
                return ResourceManager.GetString("ReasonRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The size of the reason field is limited to 4096 characters.
        /// </summary>
        public static string ReasonTooLong {
            get {
                return ResourceManager.GetString("ReasonTooLong", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your request could not be processed because your SSO identity could not be found in the directory.
        /// </summary>
        public static string SsoIdentityNotFound {
            get {
                return ResourceManager.GetString("SsoIdentityNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to grant access.
        /// </summary>
        public static string UnableToGrantAccess {
            get {
                return ResourceManager.GetString("UnableToGrantAccess", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to process request.
        /// </summary>
        public static string UnableToProcessRequest {
            get {
                return ResourceManager.GetString("UnableToProcessRequest", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An unexpected error occurred.
        /// </summary>
        public static string UnexpectedError {
            get {
                return ResourceManager.GetString("UnexpectedError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User name.
        /// </summary>
        public static string Username {
            get {
                return ResourceManager.GetString("Username", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please provide a reason for this request.
        /// </summary>
        public static string UserReasonPrompt {
            get {
                return ResourceManager.GetString("UserReasonPrompt", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Valid until.
        /// </summary>
        public static string ValidUntil {
            get {
                return ResourceManager.GetString("ValidUntil", resourceCulture);
            }
        }
    }
}
