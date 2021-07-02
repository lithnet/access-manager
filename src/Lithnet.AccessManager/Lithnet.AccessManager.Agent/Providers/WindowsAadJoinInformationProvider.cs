using Lithnet.AccessManager.Agent.Interop;
using Lithnet.AccessManager.Agent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Vanara.Extensions;
using Vanara.PInvoke;
using Vanara.Security.AccessControl;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class WindowsAadJoinInformationProvider : IAadJoinInformationProvider
    {
        private readonly ILogger<WindowsAadJoinInformationProvider> logger;
        private readonly IAgentSettings agentSettings;
        private DsRegJoinInfo joinInfo;
        private X509Certificate2 certificate;
        private SafeAccessTokenHandle impersonationContextHandle;

        public WindowsAadJoinInformationProvider(ILogger<WindowsAadJoinInformationProvider> logger, IAgentSettings agentSettings)
        {
            this.logger = logger;
            this.agentSettings = agentSettings;
        }

        public bool InitializeJoinInformation()
        {
            if (Environment.OSVersion.Version.Major < 10)
            {
                return false;
            }

            if (!agentSettings.AzureAdTenantIDs.Any())
            {
                this.logger.LogTrace("No AAD tenant ID was configured");
                return false;
            }

            foreach (string tenantId in agentSettings.AzureAdTenantIDs)
            {
                DsRegJoinInfo j = this.GetJoinInfoCurrentUser(tenantId);

                if (j == null || j.IsNull)
                {
                    continue;
                }

                if (j.JoinType != NetApi32.DSREG_JOIN_TYPE.DSREG_DEVICE_JOIN)
                {
                    continue;
                }

                this.joinInfo = j;
                this.certificate = this.joinInfo.Certificate;
                this.logger.LogTrace("Found AAD device join information");
                this.logger.LogTrace(joinInfo.ToString());
                return true;
            }

            foreach (var context in this.GetNetJoinInfoFromLoggedOnUsers(agentSettings.AzureAdTenantIDs))
            {
                if (context.JoinInfo.JoinType == NetApi32.DSREG_JOIN_TYPE.DSREG_WORKPLACE_JOIN)
                {
                    this.logger.LogTrace("Found AAD registration information");
                    this.logger.LogTrace($"Found information in session {context.SessionId} for user {context.Identity.Name}");
                    this.logger.LogTrace(context.JoinInfo.ToString());
                    this.logger.LogTrace($"Had private key while impersonating {context.HadKey}");

                    if (!context.JoinInfo.IsPrivateKeyAvailable)
                    {
                        this.logger.LogError(EventIDs.AdCertificatePrivateKeyNotAvailable, "The certificate private key for the registration is not available, the registration information will be ignored");
                        continue;
                    }

                    this.joinInfo = context.JoinInfo;
                    this.certificate = context.Certificate;
                    this.impersonationContextHandle = context.TokenHandle;
                    return true;
                }
            }

            this.logger.LogWarning(EventIDs.NoSuitableAadTenantFound, $"Could not find suitable Azure AD tenant join information for the allowed Azure AD tenants. Allowed tenants -> {string.Join(',', agentSettings.AzureAdTenantIDs)}");

            return false;
        }

        public T DelegateCertificateOperation<T>(Func<X509Certificate2, T> signingDelegate)
        {
            if (this.joinInfo == null)
            {
                throw new ComputerNotAadJoinedException();
            }

            if (this.joinInfo.JoinType == NetApi32.DSREG_JOIN_TYPE.DSREG_DEVICE_JOIN)
            {
                return signingDelegate(this.certificate);
            }

            if (this.joinInfo.JoinType == NetApi32.DSREG_JOIN_TYPE.DSREG_WORKPLACE_JOIN)
            {
                return WindowsIdentity.RunImpersonated(this.impersonationContextHandle, () => signingDelegate(this.certificate));
            }

            throw new InvalidOperationException("Could not delegate the certificate operation as the join info was not of a known type");
        }

        private DsRegJoinInfo GetJoinInfoCurrentUser(string tenantId)
        {
            NativeMethods.NetGetAadJoinInformation(tenantId, out DsRegJoinInfo joinInfo2).ThrowIfFailed();

            if (!joinInfo2.IsNull)
            {
                return joinInfo2;
            }

            return null;
        }

        public bool IsAadJoined => this.joinInfo != null;

        public bool IsWorkplaceJoined => this.joinInfo?.JoinType == NetApi32.DSREG_JOIN_TYPE.DSREG_WORKPLACE_JOIN;

        public bool IsDeviceJoined => this.joinInfo?.JoinType == NetApi32.DSREG_JOIN_TYPE.DSREG_DEVICE_JOIN;

        public string DeviceId => this.joinInfo?.DeviceId ?? throw new ComputerNotAadJoinedException();

        public string TenantId => this.joinInfo?.TenantId ?? throw new ComputerNotAadJoinedException();

        public X509Certificate2 GetAadCertificate()
        {
            return this.certificate ?? throw new ComputerNotAadJoinedException("The computer is not joined to an Azure AD");
        }

        private List<WorkplaceJoinContext> GetNetJoinInfoFromLoggedOnUsers(IEnumerable<string> tenantIds)
        {
            IntPtr ptrSessionData = IntPtr.Zero;
            List<WorkplaceJoinContext> joinContexts = new List<WorkplaceJoinContext>();

            try
            {
                Process.GetCurrentProcess().EnablePrivilege(SystemPrivilege.TrustedComputerBase);

                if (!NativeMethods.WTSEnumerateSessions((IntPtr)NativeMethods.WTS_CURRENT_SERVER_HANDLE, 0, 1, out ptrSessionData, out int sessionCount))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                int sizeOfSessionInfo = Marshal.SizeOf(typeof(WtsSessionInfo));

                IntPtr ptrSession = ptrSessionData;

                for (int i = 0; i < sessionCount; i++)
                {
                    WtsSessionInfo sessionInfo = Marshal.PtrToStructure<WtsSessionInfo>(ptrSession);

                    if (sessionInfo.SessionId > 0 && (sessionInfo.State == WtsConnectState.WtsActive || sessionInfo.State == WtsConnectState.WtsConnected || sessionInfo.State == WtsConnectState.WtsDisconnected))
                    {
                        if (!NativeMethods.WTSQueryUserToken(sessionInfo.SessionId, out IntPtr token))
                        {
                            int error = Marshal.GetLastWin32Error();
                            this.logger.LogTrace(new Win32Exception(error), $"Unable to query session {sessionInfo.SessionId}");
                        }
                        else
                        {
                            var safeToken = new SafeAccessTokenHandle(token);
                            this.logger.LogTrace($"Got token for session {sessionInfo.SessionId}");

                            try
                            {
                                WindowsIdentity.RunImpersonated(safeToken, () =>
                                {
                                    foreach (string tenantId in tenantIds)
                                    {
                                        DsRegJoinInfo j = this.GetJoinInfoCurrentUser(tenantId);
                                        if (j == null || j.IsNull)
                                        {
                                            continue;
                                        }

                                        WorkplaceJoinContext context = new WorkplaceJoinContext()
                                        {
                                            Certificate = j.Certificate,
                                            Identity = WindowsIdentity.GetCurrent(),
                                            JoinInfo = j,
                                            SessionId = sessionInfo.SessionId,
                                            TokenHandle = safeToken,
                                            HadKey = j.IsPrivateKeyAvailable
                                        };

                                        joinContexts.Add(context);
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                this.logger.LogError(EventIDs.ImpersonationFailure, ex, $"An error occurred performing the impersonation event in session {sessionInfo.SessionId}");
                            }
                        }
                    }

                    ptrSession += sizeOfSessionInfo;
                }
            }
            finally
            {
                if (ptrSessionData != IntPtr.Zero)
                {
                    NativeMethods.WTSFreeMemory(ptrSessionData);
                }
            }

            return joinContexts;
        }
    }
}