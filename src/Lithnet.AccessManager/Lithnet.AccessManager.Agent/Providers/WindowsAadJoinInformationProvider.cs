using Lithnet.AccessManager.Agent.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading.Tasks;
using Vanara.Extensions;
using Vanara.PInvoke;
using Vanara.Security.AccessControl;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class WindowsAadJoinInformationProvider : IAadJoinInformationProvider
    {
        private readonly ILogger<WindowsAadJoinInformationProvider> logger;
        private readonly IMetadataProvider metadataProvider;

        private DSREG_JOIN_INFO joinInfoCache;

        private X509Certificate2 certificate;

        public WindowsAadJoinInformationProvider(ILogger<WindowsAadJoinInformationProvider> logger, IMetadataProvider metadataProvider)
        {
            this.logger = logger;
            this.metadataProvider = metadataProvider;
        }

        private async Task<DSREG_JOIN_INFO> GetJoinInfo()
        {
            if (this.joinInfoCache != null)
            {
                return this.joinInfoCache;
            }

            
            if (Environment.OSVersion.Version.Major < 10)
            {
                throw new ComputerNotAadJoinedException();
            }

            var metadata = await metadataProvider.GetMetadata();
            bool aadrAllowed = metadata.AgentAuthentication.AllowedOptions.Contains("aadr");

            foreach (var tenantId in metadata.AgentAuthentication.AllowedAzureAdTenants)
            {
                DSREG_JOIN_INFO j = this.GetJoinInfoCurrentUser(tenantId);

                if (j != null && !j.IsNull)
                {
                    if (!aadrAllowed && j.joinType != NetApi32.DSREG_JOIN_TYPE.DSREG_DEVICE_JOIN)
                    {
                        continue;
                    }

                    this.joinInfoCache = j;
                    return j;
                }
            }

            this.logger.LogWarning($"Could not find suitable Azure AD tenant join information for the allowed Azure AD tenants. Allowed tenants -> {string.Join(',', metadata.AgentAuthentication.AllowedAzureAdTenants)}");

            throw new ComputerNotAadJoinedException();
        }

        private DSREG_JOIN_INFO GetJoinInfoCurrentUser(string tenantId)
        {
            NativeMethods.NetGetAadJoinInformation(tenantId, out DSREG_JOIN_INFO joinInfo2).ThrowIfFailed();

            if (!joinInfo2.IsNull)
            {
                this.logger.LogTrace("Got AAD join information");
                this.logger.LogTrace($"Device ID: {joinInfo2.pszDeviceId}\r\n" +
                                     $"Domain: {joinInfo2.pszIdpDomain}\r\n" +
                                     $"Join type: {joinInfo2.joinType} \r\n" +
                                     $"Tenant Name: {joinInfo2.pszTenantDisplayName}\r\n" +
                                     $"Tenant ID: {joinInfo2.pszTenantId}\r\n");

                return joinInfo2;
            }

            return null;
        }

        public async Task<string> GetDeviceId()
        {
            var joinInfo = await this.GetJoinInfo();

            return joinInfo.pszDeviceId;
        }

        public async Task<string> GetTenantId()
        {
            var joinInfo = await this.GetJoinInfo();

            return joinInfo.pszTenantId;
        }

        public async Task<X509Certificate2> GetAadCertificate()
        {
            if (this.certificate != null)
            {
                return this.certificate;
            }

            var joinInfo = await this.GetJoinInfo();

            if (joinInfo == null || joinInfo.IsNull)
            {
                throw new ComputerNotAadJoinedException("The computer is not joined to an Azure AD");
            }

            if (!joinInfo.pJoinCertificate.HasValue)
            {
                throw new ComputerNotAadJoinedException("The computer did not have an AAD certificate");
            }

            this.certificate = joinInfo.GetCertificate();

            return this.certificate;
        }

        private async Task<DSREG_JOIN_INFO> FindRegistrationInfoFromLocalUsers()
        {
            Principal queryFilter = new UserPrincipal(new PrincipalContext(ContextType.Machine));

            PrincipalSearcher searcher = new PrincipalSearcher(queryFilter);
            var results = searcher.FindAll();

            foreach (var principal in results.OfType<UserPrincipal>())
            {
                try
                {
                    if (!principal.Enabled.HasValue || !principal.Enabled.Value || principal.Sid.IsWellKnown(WellKnownSidType.AccountGuestSid))
                    {
                        continue;
                    }

                    this.logger.LogInformation($"Searching user profile {principal.SamAccountName} for join information");

                    var info = await GetJoinInfoForUser(principal.SamAccountName);

                    if (info != null && !info.IsNull)
                    {
                        return info;
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"Could not determine registration status for {principal.SamAccountName}");
                }
            }

            return null;
        }

        private async Task<DSREG_JOIN_INFO> GetJoinInfoForUser(string username)
        {
            var currentProcess = Process.GetCurrentProcess();
            currentProcess.EnablePrivilege(SystemPrivilege.TrustedComputerBase);
            currentProcess.EnablePrivilege(SystemPrivilege.Backup);
            currentProcess.EnablePrivilege(SystemPrivilege.Restore);

            Secur32.LsaRegisterLogonProcess("AccessManagerAgent", out Secur32.SafeLsaConnectionHandle handle, out uint mode).ThrowIfFailed();
            Secur32.LsaLookupAuthenticationPackage(handle, "MICROSOFT_AUTHENTICATION_PACKAGE_V1_0", out uint authPackage).ThrowIfFailed();

            string domainName = Environment.MachineName;

            int usernameLength = username.Length * sizeof(char);
            int domainLength = domainName.Length * sizeof(char);
            int authInfoLength = (Marshal.SizeOf(typeof(MSV1_0_S4U_LOGON)) + usernameLength + domainLength);
            IntPtr authInfo = Marshal.AllocHGlobal((int)authInfoLength);

            try
            {
                IntPtr usernamePtr = IntPtr.Add(authInfo, Marshal.SizeOf(typeof(MSV1_0_S4U_LOGON)));
                IntPtr domainPtr = IntPtr.Add(usernamePtr, usernameLength);

                MSV1_0_S4U_LOGON l = new MSV1_0_S4U_LOGON
                {
                    UserPrincipalName = new AdvApi32.LSA_UNICODE_STRING
                    {
                        Buffer = usernamePtr,
                        length = (UInt16)usernameLength,
                        MaximumLength = (UInt16)usernameLength
                    },
                    DomainName = new AdvApi32.LSA_UNICODE_STRING
                    {
                        Buffer = domainPtr,
                        length = (UInt16)domainLength,
                        MaximumLength = (UInt16)domainLength
                    },
                    MessageType = 12 //Secur32.MSV1_0_LOGON_SUBMIT_TYPE.MsV1_0S4ULogon
                };

                Marshal.StructureToPtr(l, authInfo, false);
                Marshal.Copy(username.ToCharArray(), 0, usernamePtr, username.Length);
                Marshal.Copy(domainName.ToCharArray(), 0, domainPtr, domainName.Length);

                var tokenSource = new AdvApi32.TOKEN_SOURCE();

                tokenSource.SourceName = "myapp123".ToCharArray();
                AdvApi32.AllocateLocallyUniqueId(out AdvApi32.LUID luid);
                tokenSource.SourceIdentifier = luid;

                var result = Secur32.LsaLogonUser(
                    handle,
                    "ams",
                    Secur32.SECURITY_LOGON_TYPE.Network,
                    authPackage,
                    authInfo,
                    (uint)authInfoLength,
                    IntPtr.Zero,
                    tokenSource,
                    out var profileBuffer,
                    out var profileBufferLength,
                    out var logonId,
                    out var token,
                    out var quotes,
                    out var substatus);

                result.ThrowIfFailed();

                if (!AdvApi32.DuplicateTokenEx(token.DangerousGetHandle(), 0, null, AdvApi32.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation, AdvApi32.TOKEN_TYPE.TokenPrimary, out var newToken))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error);
                }

                WindowsIdentity d = new WindowsIdentity(newToken.DangerousGetHandle());
                bool loadedProfile = false;

                UserEnv.PROFILEINFO profileInfo = new UserEnv.PROFILEINFO();
                profileInfo.lpUserName = username;
                profileInfo.dwSize = (uint)Marshal.SizeOf(profileInfo);

                var metadata = await metadataProvider.GetMetadata();

                try
                {
                    logger.LogInformation($"My username is {Environment.UserName}");
                    if (!UserEnv.LoadUserProfile(newToken.DangerousGetHandle(), ref profileInfo))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error);
                    }

                    loadedProfile = true;
                    logger.LogInformation("Loaded user profile");

                    DSREG_JOIN_INFO Find()
                    {
                        var data = this.GetJoinInfoCurrentUser(null);

                        if (data != null && !data.IsNull)
                        {
                            var cert = data.GetCertificate();
                            X509Store s = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                            s.Open(OpenFlags.ReadOnly);
                            var res = s.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, false);

                            var dd = res[0];
                        }

                        return data;
                    }

                    return WindowsIdentity.RunImpersonated(
                        new Microsoft.Win32.SafeHandles.SafeAccessTokenHandle(newToken.DangerousGetHandle()),
                        Find);
                }
                finally
                {
                    if (loadedProfile)
                    {
                        UserEnv.UnloadUserProfile(token.DangerousGetHandle(), profileInfo.hProfile);
                    }
                }
            }
            finally
            {
                if (authInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(authInfo);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct MSV1_0_S4U_LOGON
    {
        public uint MessageType;
        public uint Flags;
        public AdvApi32.LSA_UNICODE_STRING UserPrincipalName; // username or username@domain
        public AdvApi32.LSA_UNICODE_STRING DomainName; // Optional: if missing, using the local machine
    }
}