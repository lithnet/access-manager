using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Agent.Interop
{
    public class DSREG_JOIN_INFO : SafeHANDLE
    {
        /// <summary>An enumeration value that specifies the type of the join.</summary>
        public NetApi32.DSREG_JOIN_TYPE joinType => this.Value.joinType;

        /// <summary>
        /// Representations of the certification for the join. This is a pointer to <c>CERT_CONTEXT</c> structure which can be found in <c>Vanara.PInvoke.Cryptography</c>.
        /// </summary>
        public Crypt32.CERT_CONTEXT? pJoinCertificate => this.Value.pJoinCertificate.ToNullableStructure<Crypt32.CERT_CONTEXT>();

        /// <summary>The PSZ device identifier</summary>
        public string pszDeviceId => this.Value.pszDeviceId;

        /// <summary>A string that represents Azure Active Directory (Azure AD).</summary>
        public string pszIdpDomain => this.Value.pszIdpDomain;

        /// <summary>The identifier of the joined Azure AD tenant.</summary>
        public string pszTenantId => this.Value.pszTenantId;

        /// <summary>The email address for the joined account.</summary>
        public string pszJoinUserEmail => this.Value.pszJoinUserEmail;

        /// <summary>The display name for the joined account.</summary>
        public string pszTenantDisplayName => this.Value.pszTenantDisplayName;

        /// <summary>The URL to use to enroll in the Mobile Device Management (MDM) service.</summary>
        public string pszMdmEnrollmentUrl => this.Value.pszMdmEnrollmentUrl;

        /// <summary>The URL that provides information about the terms of use for the MDM service.</summary>
        public string pszMdmTermsOfUseUrl => this.Value.pszMdmTermsOfUseUrl;

        /// <summary>The URL that provides information about compliance for the MDM service.</summary>
        public string pszMdmComplianceUrl => this.Value.pszMdmComplianceUrl;

        /// <summary>The URL for synchronizing user settings.</summary>
        public string pszUserSettingSyncUrl => this.Value.pszUserSettingSyncUrl;

        /// <summary>Information about the user account that was used to join a device to Azure AD.</summary>
        public NetApi32.DSREG_USER_INFO? pUserInfo => this.Value.pUserInfo.ToNullableStructure<NetApi32.DSREG_USER_INFO>();

        public X509Certificate2 GetCertificate()
        {
            return new X509Certificate2(this.Value.pJoinCertificate);
        }

        /// <summary>
        /// Internal method that actually releases the handle. This is called by <see cref="M:Vanara.PInvoke.SafeHANDLE.ReleaseHandle"/>
        /// for valid handles and afterwards zeros the handle.
        /// </summary>
        /// <returns><c>true</c> to indicate successful release of the handle; <c>false</c> otherwise.</returns>
        protected override bool InternalReleaseHandle()
        {
            NativeMethods.NetFreeAadJoinInformation(this.handle);
            return true;
        }

        private _DSREG_JOIN_INFO Value => this.handle.ToStructure<_DSREG_JOIN_INFO>();

        [StructLayout(LayoutKind.Sequential)]
        struct _DSREG_JOIN_INFO
        {
            public NetApi32.DSREG_JOIN_TYPE joinType;
            public IntPtr pJoinCertificate;
            public StrPtrUni pszDeviceId;
            public StrPtrUni pszIdpDomain;
            public StrPtrUni pszTenantId;
            public StrPtrUni pszJoinUserEmail;
            public StrPtrUni pszTenantDisplayName;
            public StrPtrUni pszMdmEnrollmentUrl;
            public StrPtrUni pszMdmTermsOfUseUrl;
            public StrPtrUni pszMdmComplianceUrl;
            public StrPtrUni pszUserSettingSyncUrl;
            public IntPtr pUserInfo;
        }
    }
}
