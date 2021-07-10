using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Api.Shared
{
    public static class ApiConstants
    {
        public const string BadRequest = "bad-request";
        public const string TokenValidationFailed = "token-validation-failed";
        public const string DeviceNotFound = "device-not-found";
        public const string DeviceDisabled = "device-disabled";
        public const string DeviceNotApproved = "device-not-approved";
        public const string AadDeviceNotFound = "aad-device-not-found";
        public const string UnsupportedAuthType = "unsupported-auth-type";
        public const string RollbackDenied = "rollback-denied";
        public const string RegistrationDisabled = "registration-disabled";
        public const string InvalidRegistrationKey = "invalid-registration-key";
        public const string InternalError = "internal-error";
        public const string DeviceCredentialsNotFound = "device-credentials-not-found";
    }
}

