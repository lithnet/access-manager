using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Agent.Interop
{
    public class DsRegJoinInfo : SafeHANDLE
    {
        private X509Certificate2 certificate;

        /// <summary>An enumeration value that specifies the type of the join.</summary>
        public NetApi32.DSREG_JOIN_TYPE JoinType => this.Value.joinType;

        /// <summary>
        /// Representations of the certification for the join. This is a pointer to <c>CERT_CONTEXT</c> structure which can be found in <c>Vanara.PInvoke.Cryptography</c>.
        /// </summary>
        public Crypt32.CERT_CONTEXT? JoinCertificateContext => this.Value.pJoinCertificate.ToNullableStructure<Crypt32.CERT_CONTEXT>();

        public X509Certificate2 Certificate
        {
            get
            {
                if (this.certificate == null && this.JoinCertificateContext.HasValue)
                {
                    this.certificate = new X509Certificate2(this.Value.pJoinCertificate);
                }

                return this.certificate;
            }
        }

        public bool IsPrivateKeyAvailable
        {
            get
            {
                try
                {
                    if (this.Certificate.HasPrivateKey)
                    {
                        _ = this.Certificate.PrivateKey;
                        return true;
                    }
                }
                catch
                {
                    // ignore
                }

                return false;
            }
        }

        /// <summary>The device identifier</summary>
        public string DeviceId => this.Value.pszDeviceId;

        /// <summary>A string that represents Azure Active Directory (Azure AD).</summary>
        public string IdpDomain => this.Value.pszIdpDomain;

        /// <summary>The identifier of the joined Azure AD tenant.</summary>
        public string TenantId => this.Value.pszTenantId;

        /// <summary>The email address for the joined account.</summary>
        public string JoinUserEmail => this.Value.pszJoinUserEmail;

        /// <summary>The display name for the joined account.</summary>
        public string TenantDisplayName => this.Value.pszTenantDisplayName;

        /// <summary>The URL to use to enroll in the Mobile Device Management (MDM) service.</summary>
        public string MdmEnrollmentUrl => this.Value.pszMdmEnrollmentUrl;

        /// <summary>The URL that provides information about the terms of use for the MDM service.</summary>
        public string MdmTermsOfUseUrl => this.Value.pszMdmTermsOfUseUrl;

        /// <summary>The URL that provides information about compliance for the MDM service.</summary>
        public string MdmComplianceUrl => this.Value.pszMdmComplianceUrl;

        /// <summary>The URL for synchronizing user settings.</summary>
        public string UserSettingSyncUrl => this.Value.pszUserSettingSyncUrl;

        /// <summary>Information about the user account that was used to join a device to Azure AD.</summary>
        public NetApi32.DSREG_USER_INFO? UserInfo => this.Value.pUserInfo.ToNullableStructure<NetApi32.DSREG_USER_INFO>();

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

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Join type: {this.JoinType}");
            builder.AppendLine($"Tenant ID: {this.TenantId}");
            builder.AppendLine($"Tenant Name: {this.TenantDisplayName}");
            builder.AppendLine($"IDP domain: {this.IdpDomain}");
            builder.AppendLine($"Join user: {this.JoinUserEmail}");
            builder.AppendLine($"Certificate thumbprint: {this.Certificate?.Thumbprint}");
            return builder.ToString();
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
