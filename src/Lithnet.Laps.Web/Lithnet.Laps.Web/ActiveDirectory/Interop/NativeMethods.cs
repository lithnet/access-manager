using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.InteropServices;
using System.Security.Principal;
using NLog;

namespace Lithnet.Laps.Web.ActiveDirectory.Interop
{
    internal static class NativeMethods
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const int InsufficientBuffer = 122;

        private static int directoryReferralLimit = -1;

        private const string AuthzObjectUuidWithcap = "9a81c2bd-a525-471d-a4ed-49907c0b23da";

        private const string RcpOverTcpProtocol = "ncacn_ip_tcp";

        private static SecurityIdentifier currentDomainSid;

        private static SecurityIdentifier CurrentDomainSid
        {
            get
            {
                if (NativeMethods.currentDomainSid == null)
                {
                    Domain domain = Domain.GetComputerDomain();
                    NativeMethods.currentDomainSid = new SecurityIdentifier((byte[])(domain.GetDirectoryEntry().Properties["objectSid"][0]), 0);
                }

                return NativeMethods.currentDomainSid;
            }
        }

        private static int DirectoryReferralLimit
        {
            get
            {
                if (NativeMethods.directoryReferralLimit < 0)
                {
                    if (!int.TryParse(ConfigurationManager.AppSettings["directory:referral-limit"] ?? "10", out NativeMethods.directoryReferralLimit))
                    {
                        NativeMethods.directoryReferralLimit = 10;
                    }
                }

                return NativeMethods.directoryReferralLimit;
            }
        }

        [DllImport("Ntdsapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int DsBind(string domainControllerName, string dnsDomainName, out IntPtr hds);

        [DllImport("Ntdsapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int DsUnBind(IntPtr hds);

        [DllImport("ntdsapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DsCrackNames(IntPtr hds, DsNameFlags flags, DsNameFormat formatOffered, DsNameFormat formatDesired, uint cNames, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPTStr, SizeParamIndex = 4)] string[] rpNames, out IntPtr ppResult);

        [DllImport("ntdsapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void DsFreeNameResult(IntPtr pResult);

        [DllImport("NetApi32.dll", CharSet = CharSet.Unicode, SetLastError =true)]
        private static extern int DsGetDcName(string computerName, string domainName, IntPtr domainGuid, string siteName, DsGetDcNameFlags flags, out IntPtr domainControllerInfo);

        [DllImport("NetApi32.dll", EntryPoint = "NetApiBufferFree")]
        private static extern int NetApiFreeBuffer(IntPtr buffer);

        [DllImport("authz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzInitializeRemoteResourceManager(IntPtr rpcInitInfo, out IntPtr authRm);

        [DllImport("authz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzInitializeContextFromSid(AuthzInitFlags flags, byte[] rawUserSid, IntPtr authzRm, IntPtr expirationTime, Luid identifier, IntPtr dynamicGroupArgs, out IntPtr authzClientContext);

        [DllImport("authz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzInitializeResourceManager(AuthzResourceManagerFlags flags, IntPtr pfnAccessCheck, IntPtr pfnComputeDynamicGroups, IntPtr pfnFreeDynamicGroups,
            string szResourceManagerName, out IntPtr phAuthzResourceManager);

        [DllImport("authz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzFreeContext(IntPtr authzClientContext);

        [DllImport("authz.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzFreeResourceManager(IntPtr handle);

        [DllImport("authz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AuthzGetInformationFromContext(IntPtr hAuthzClientContext, AuthzContextInformationClass infoClass, uint bufferSize, out uint pSizeRequired, IntPtr buffer);

        public static string GetDnFromGc(string nameToFind, string dnsDomainName = null, int referralLevel = 0)
        {
            IntPtr hds = IntPtr.Zero;

            try
            {
                int result = NativeMethods.DsBind(null, dnsDomainName, out hds);
                if (result != 0)
                {
                    throw new Win32Exception(result);
                }

                DsNameResultItem nameResult = NativeMethods.CrackNames(hds, DsNameFlags.DS_NAME_FLAG_TRUST_REFERRAL, DsNameFormat.DS_UNKNOWN_NAME, DsNameFormat.DS_FQDN_1779_NAME, nameToFind);

                switch (nameResult.Status)
                {
                    case DsNameError.None:
                        return nameResult.Name;

                    case DsNameError.NoMapping:
                        throw new InvalidOperationException($"The object name {nameToFind} was found in the global catalog, but could not be mapped to a DN");

                    case DsNameError.TrustReferral:
                    case DsNameError.DomainOnly:
                        if (!string.IsNullOrWhiteSpace(nameResult.Domain))
                        {
                            if (referralLevel < NativeMethods.DirectoryReferralLimit)
                            {
                                return NativeMethods.GetDnFromGc(nameToFind, nameResult.Domain, ++referralLevel);
                            }

                            throw new InvalidOperationException("The referral limit exceeded the maximum configured value");
                        }

                        throw new NotFoundException($"A referral to the object name {nameToFind} was received from the global catalog, but no referral information was provided. DsNameError: {nameResult.Status}");

                    case DsNameError.NotFound:
                        throw new NotFoundException($"The object name {nameToFind} was not found in the global catalog");

                    case DsNameError.NotUnique:
                        throw new InvalidOperationException($"There was more than one object with the name {nameToFind} in the global catalog");

                    case DsNameError.Resolving:
                        throw new InvalidOperationException($"The object name {nameToFind} was not able to be searched in the global catalog");

                    case DsNameError.NoSyntacticalMapping:
                        throw new ArgumentException($"DsCrackNames unexpectedly returned DS_NAME_ERROR_NO_SYNTACTICAL_MAPPING for name {nameToFind}");
                }

                return nameResult.Name;
            }
            finally
            {
                if (hds != IntPtr.Zero)
                {
                    NativeMethods.DsUnBind(hds);
                }
            }
        }

        public static bool CheckForSidInToken(SecurityIdentifier principalSid, SecurityIdentifier sidToCheck, SecurityIdentifier requestContext = null)
        {
            if (principalSid == null)
            {
                throw new ArgumentNullException(nameof(principalSid));
            }

            if (sidToCheck == null)
            {
                throw new ArgumentNullException(nameof(sidToCheck));
            }

            string server;

            if (requestContext == null || requestContext.IsEqualDomainSid(NativeMethods.CurrentDomainSid))
            {
                server = null;
            }
            else
            {
                string dnsDomain = NativeMethods.GetDnsDomainNameFromSid(requestContext.AccountDomainSid);
                server = NativeMethods.GetDomainControllerForDnsDomain(dnsDomain);
            }

            return NativeMethods.CheckForSidInToken(principalSid, sidToCheck, server);
        }

        private static string GetDomainControllerForDnsDomain(string dnsDomain, bool forceRediscovery = false)
        {
            IntPtr pdcInfo = IntPtr.Zero;

            try
            {
                int result = DsGetDcName(
                    null,
                    dnsDomain,
                    IntPtr.Zero,
                    null, 
                    DsGetDcNameFlags.DS_DIRECTORY_SERVICE_8_REQUIRED | (forceRediscovery ? DsGetDcNameFlags.DS_FORCE_REDISCOVERY : 0),
                    out pdcInfo);

                if (result != 0)
                {
                    throw new Win32Exception(result);
                }

                DomainControllerInfo info = Marshal.PtrToStructure<DomainControllerInfo>(pdcInfo);

                return info.DomainControllerName.TrimStart('\\');
            }
            finally
            {
                if (pdcInfo != IntPtr.Zero)
                {
                    NetApiFreeBuffer(pdcInfo);
                }
            }
        }

        private static string GetDnsDomainNameFromSid(SecurityIdentifier sid, int referralLevel = 0)
        {
            IntPtr hds = IntPtr.Zero;

            try
            {
                int result = NativeMethods.DsBind(null, null, out hds);
                if (result != 0)
                {
                    throw new Win32Exception(result);
                }

                string nameToFind = sid.Value;

                DsNameResultItem nameResult = NativeMethods.CrackNames(hds, DsNameFlags.DS_NAME_FLAG_TRUST_REFERRAL, DsNameFormat.DS_SID_OR_SID_HISTORY_NAME, DsNameFormat.DS_NT4_ACCOUNT_NAME, nameToFind);

                switch (nameResult.Status)
                {
                    case DsNameError.None:
                        return nameResult.Domain;

                    case DsNameError.NoMapping:
                        throw new InvalidOperationException($"The object name {nameToFind} was found in the global catalog, but could not be mapped to a DN");

                    case DsNameError.TrustReferral:
                    case DsNameError.DomainOnly:
                        if (!string.IsNullOrWhiteSpace(nameResult.Domain))
                        {
                            if (referralLevel < NativeMethods.DirectoryReferralLimit)
                            {
                                return NativeMethods.GetDnFromGc(nameToFind, nameResult.Domain, ++referralLevel);
                            }

                            throw new InvalidOperationException("The referral limit exceeded the maximum configured value");
                        }

                        throw new NotFoundException($"A referral to the object name {nameToFind} was received from the global catalog, but no referral information was provided. DsNameError: {nameResult.Status}");

                    case DsNameError.NotFound:
                        throw new NotFoundException($"The object name {nameToFind} was not found in the global catalog");

                    case DsNameError.NotUnique:
                        throw new InvalidOperationException($"There was more than one object with the name {nameToFind} in the global catalog");

                    case DsNameError.Resolving:
                        throw new InvalidOperationException($"The object name {nameToFind} was not able to be searched in the global catalog");

                    case DsNameError.NoSyntacticalMapping:
                        throw new ArgumentException($"DsCrackNames unexpectedly returned DS_NAME_ERROR_NO_SYNTACTICAL_MAPPING for name {nameToFind}");
                }

                return nameResult.Name;
            }
            finally
            {
                if (hds != IntPtr.Zero)
                {
                    NativeMethods.DsUnBind(hds);
                }
            }
        }

        private static DsNameResultItem CrackNames(IntPtr hds, DsNameFlags flags, DsNameFormat formatOffered, DsNameFormat formatDesired, string name)
        {
            DsNameResultItem[] resultItems = NativeMethods.CrackNames(hds, flags, formatOffered, formatDesired, new[] { name });
            return resultItems[0];
        }

        private static DsNameResultItem[] CrackNames(IntPtr hds, DsNameFlags flags, DsNameFormat formatOffered, DsNameFormat formatDesired, string[] namesToCrack)
        {
            IntPtr pDsNameResult = IntPtr.Zero;
            DsNameResultItem[] resultItems;

            try
            {
                uint namesToCrackCount = (uint)namesToCrack.Length;

                int result = NativeMethods.DsCrackNames(hds, flags, formatOffered, formatDesired, namesToCrackCount, namesToCrack, out pDsNameResult);

                if (result != 0)
                {
                    throw new Win32Exception(result);
                }

                DsNameResult dsNameResult = (DsNameResult)Marshal.PtrToStructure(pDsNameResult, typeof(DsNameResult));

                if (dsNameResult.cItems == 0)
                {
                    throw new InvalidOperationException("DsCrackNames returned an unexpected result");
                }

                resultItems = new DsNameResultItem[dsNameResult.cItems];
                IntPtr pItem = dsNameResult.rItems;

                for (int idx = 0; idx < dsNameResult.cItems; idx++)
                {
                    resultItems[idx] = (DsNameResultItem)Marshal.PtrToStructure(pItem, typeof(DsNameResultItem));
                    pItem = IntPtr.Add(pItem, Marshal.SizeOf(resultItems[idx]));
                }
            }
            finally
            {
                if (pDsNameResult != IntPtr.Zero)
                {
                    NativeMethods.DsFreeNameResult(pDsNameResult);
                }
            }

            return resultItems;
        }

        private static bool CheckForSidInToken(SecurityIdentifier principalSid, SecurityIdentifier sidToCheck, string serverName = null)
        {
            if (principalSid == sidToCheck)
            {
                return true;
            }

            foreach (SecurityIdentifier sid in NativeMethods.GetTokenGroups(principalSid, serverName))
            {
                if (sid == sidToCheck)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<SecurityIdentifier> GetTokenGroups(SecurityIdentifier principalSid, string authzServerName = null)
        {
            IntPtr userClientCtxt = IntPtr.Zero;
            IntPtr pstructure = IntPtr.Zero;
            IntPtr pClientInfo = IntPtr.Zero;
            IntPtr authzRm = IntPtr.Zero;

            try
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(authzServerName))
                    {
                        AuthzRpcInitInfoClient client = new AuthzRpcInitInfoClient
                        {
                            Version = AuthzRpcClientVersion.V1,
                            ObjectUuid = NativeMethods.AuthzObjectUuidWithcap,
                            Protocol = NativeMethods.RcpOverTcpProtocol,
                            Server = authzServerName
                        };

                        pClientInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(AuthzRpcInitInfoClient)));
                        Marshal.StructureToPtr(client, pClientInfo, false);

                        if (!NativeMethods.AuthzInitializeRemoteResourceManager(pClientInfo, out authzRm))
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                    }
                }
                catch (Exception ex)
                {
                    NativeMethods.logger.Warn(ex, $"Unable to connect to the remote server {authzServerName} to generate the authorization token for principal {principalSid}. The local server will be used instead, however the token generated may not contain authorization groups from other domains");
                }

                if (authzRm == IntPtr.Zero)
                {
                    if (!NativeMethods.AuthzInitializeResourceManager(AuthzResourceManagerFlags.NO_AUDIT, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, null, out authzRm))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }

                byte[] sidBytes = new byte[principalSid.BinaryLength];
                principalSid.GetBinaryForm(sidBytes, 0);

                if (!NativeMethods.AuthzInitializeContextFromSid(AuthzInitFlags.Default, sidBytes, authzRm, IntPtr.Zero, Luid.NullLuid, IntPtr.Zero, out userClientCtxt))
                {
                    int errorCode = Marshal.GetLastWin32Error();

                    if (errorCode == 5)
                    {
                        throw new Win32Exception(errorCode, "Access was denied. Please ensure that \r\n1) The service account is a member of the built-in group called 'Windows Authorization Access Group' in the domain where the computer object is located\r\n2) The service account is a member of the built-in group called 'Access Control Assistance Operators' in the domain where the computer object is located");
                    }

                    throw new Win32Exception(errorCode);
                }

                uint sizeRequired = 0;

                if (!NativeMethods.AuthzGetInformationFromContext(userClientCtxt, AuthzContextInformationClass.AuthzContextInfoGroupsSids, sizeRequired, out sizeRequired, IntPtr.Zero))
                {
                    Win32Exception e = new Win32Exception(Marshal.GetLastWin32Error());

                    if (e.NativeErrorCode != NativeMethods.InsufficientBuffer)
                    {
                        throw e;
                    }
                }

                pstructure = Marshal.AllocHGlobal((int)sizeRequired);

                if (!NativeMethods.AuthzGetInformationFromContext(userClientCtxt, AuthzContextInformationClass.AuthzContextInfoGroupsSids, sizeRequired, out sizeRequired, pstructure))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                TokenGroups groups = Marshal.PtrToStructure<TokenGroups>(pstructure);

                // Set the pointer to the first Groups array item in the structure
                IntPtr current = IntPtr.Add(pstructure, Marshal.OffsetOf<TokenGroups>(nameof(groups.Groups)).ToInt32());

                for (int i = 0; i < groups.GroupCount; i++)
                {
                    SidAndAttributes sidAndAttributes = (SidAndAttributes)Marshal.PtrToStructure(current, typeof(SidAndAttributes));
                    yield return new SecurityIdentifier(sidAndAttributes.Sid);
                    current = IntPtr.Add(current, Marshal.SizeOf(typeof(SidAndAttributes)));
                }
            }
            finally
            {
                if (pstructure != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pstructure);
                }

                if (userClientCtxt != IntPtr.Zero)
                {
                    NativeMethods.AuthzFreeContext(userClientCtxt);
                }

                if (authzRm != IntPtr.Zero)
                {
                    NativeMethods.AuthzFreeResourceManager(authzRm);
                }

                if (pClientInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pClientInfo);
                }
            }
        }
    }
}