using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Lithnet.Security.Authorization;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Interop
{
    internal static class NativeMethods
    {
        private static SecurityIdentifier localMachineSid;

        public static int DirectoryReferralLimit { get; set; } = 10;

        internal const int InsufficientBuffer = 122;

        private const int ErrorMoreData = 234;

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

        [DllImport("Ntdsapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int DsBind(string domainControllerName, string dnsDomainName, out IntPtr hds);

        [DllImport("Ntdsapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int DsUnBind(IntPtr hds);

        [DllImport("ntdsapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DsCrackNames(IntPtr hds, DsNameFlags flags, DsNameFormat formatOffered, DsNameFormat formatDesired, uint cNames, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPTStr, SizeParamIndex = 4)] string[] rpNames, out IntPtr ppResult);

        [DllImport("ntdsapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void DsFreeNameResult(IntPtr pResult);

        [DllImport("NetApi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DsGetDcName(string computerName, string domainName, IntPtr domainGuid, string siteName, DsGetDcNameFlags flags, out IntPtr domainControllerInfo);

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int NetServerGetInfo(string serverName, int level, out IntPtr pServerInfo);

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int NetWkstaGetInfo(string serverName, int level, out IntPtr pWorkstationInfo);

        [DllImport("NetApi32.dll")]
        private static extern int NetApiBufferFree(IntPtr buffer);

        [DllImport("NetAPI32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int NetLocalGroupGetMembers([MarshalAs(UnmanagedType.LPWStr)] string servername, [MarshalAs(UnmanagedType.LPWStr)] string localgroupname, int level, out IntPtr bufptr, int prefmaxlen, out int entriesread, out int totalentries, IntPtr resume_handle);

        [DllImport("netapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int NetLocalGroupAddMember(string server, string groupName, IntPtr sid);

        [DllImport("netapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int NetLocalGroupDelMember(string server, string groupName, IntPtr sid);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateWellKnownSid(WellKnownSidType wellKnownSidType, IntPtr domainSid, IntPtr pSid, ref int cbSid);

        [DllImport("Advapi32.dll", SetLastError = true, PreserveSig = true, CharSet = CharSet.Unicode)]
        private static extern int LsaQueryInformationPolicy(IntPtr pPolicyHandle, PolicyInformationClass informationClass, out IntPtr pData);

        [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true, CharSet = CharSet.Unicode)]
        private static extern int LsaOpenPolicy([In] in IntPtr pSystemName, [In] in LsaObjectAttributes objectAttributes, LsaAccessPolicy desiredAccess, out IntPtr pPolicyHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int LsaClose(IntPtr hPolicy);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int LsaNtStatusToWinError(int status);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int LsaFreeMemory(IntPtr buffer);

        public static SecurityIdentifier GetLocalMachineAuthoritySid()
        {
            if (localMachineSid != null)
            {
                return localMachineSid;
            }

            IntPtr pPolicyHandle = IntPtr.Zero;
            IntPtr pPolicyData = IntPtr.Zero;

            try
            {
                LsaObjectAttributes lsaObjectAttributes = new LsaObjectAttributes();

                var result = LsaOpenPolicy(in IntPtr.Zero, in lsaObjectAttributes, LsaAccessPolicy.PolicyViewLocalInformation, out pPolicyHandle);

                if (result != 0)
                {
                    result = LsaNtStatusToWinError(result);
                    throw new DirectoryException("LsaOpenPolicy failed", new Win32Exception(result));
                }

                result = LsaQueryInformationPolicy(pPolicyHandle, PolicyInformationClass.PolicyAccountDomainInformation, out pPolicyData);

                if (result != 0)
                {
                    result = LsaNtStatusToWinError(result);
                    throw new DirectoryException("LsaQueryInformationPolicy failed", new Win32Exception(result));
                }

                PolicyAccountDomainInfo info = Marshal.PtrToStructure<PolicyAccountDomainInfo>(pPolicyData);

                localMachineSid = new SecurityIdentifier(info.DomainSid);

                return localMachineSid;
            }
            finally
            {
                if (pPolicyData != IntPtr.Zero)
                {
                    LsaFreeMemory(pPolicyData);
                }

                if (pPolicyHandle != IntPtr.Zero)
                {
                    LsaClose(pPolicyHandle);
                }
            }
        }

        public static SecurityIdentifier CreateWellKnownSid(WellKnownSidType sidType)
        {
            return new SecurityIdentifier(sidType, GetLocalMachineAuthoritySid());
        }

        //public static SecurityIdentifier CreateWellKnownSid(WellKnownSidType sidType, SecurityIdentifier domainSid)
        //{
        //    IntPtr pSid = IntPtr.Zero;
        //    IntPtr pDomainSid = IntPtr.Zero;
            
        //    try
        //    {
        //        int pSidLength = 0;
        //        string sidString = string.Empty;

        //        pDomainSid = Marshal.AllocHGlobal(domainSid.BinaryLength);
        //        byte[] bDomainSid = new byte[domainSid.BinaryLength];
        //        domainSid.GetBinaryForm(bDomainSid, 0);
        //        Marshal.Copy(bDomainSid, 0, pDomainSid, bDomainSid.Length);

        //        if (!CreateWellKnownSid(sidType, pDomainSid, pSid, ref pSidLength))
        //        {
        //            var result = Marshal.GetLastWin32Error();

        //            if (result != InsufficientBuffer)
        //            {
        //                throw new DirectoryException("CreateWellKnownSid failed", new Win32Exception(result));
        //            }
        //        }
        //        else
        //        {
        //            throw new DirectoryException("CreateWellKnownSid should have failed");
        //        }

        //        pSid = Marshal.AllocHGlobal(pSidLength);
        //        if (!NativeMethods.CreateWellKnownSid(sidType, pDomainSid, pSid, ref pSidLength))
        //        {
        //            throw new DirectoryException("CreateWellKnownSid failed", new Win32Exception(Marshal.GetLastWin32Error()));
        //        }

        //        return new SecurityIdentifier(pSid);
        //    }
        //    finally
        //    {
        //        if (pDomainSid != IntPtr.Zero)
        //        {
        //            Marshal.FreeHGlobal(pDomainSid);
        //        }

        //        if (pSid != IntPtr.Zero)
        //        {
        //            Marshal.FreeHGlobal(pSid);
        //        }
        //    }

        //}

        public static void AddLocalGroupMember(string groupName, SecurityIdentifier sid)
        {
            var sidBytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBytes, 0);

            IntPtr pSid = Marshal.AllocHGlobal(sidBytes.Length);

            try
            {
                Marshal.Copy(sidBytes, 0, pSid, sidBytes.Length);

                var result = NetLocalGroupAddMember(null, groupName, pSid);

                if (result != 0)
                {
                    throw new DirectoryException("NetLocalGroupAddMember failed", new Win32Exception(result));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pSid);
            }
        }

        public static void RemoveLocalGroupMember(string groupName, SecurityIdentifier sid)
        {
            var sidBytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidBytes, 0);

            IntPtr pSid = Marshal.AllocHGlobal(sidBytes.Length);

            try
            {
                Marshal.Copy(sidBytes, 0, pSid, sidBytes.Length);

                var result = NetLocalGroupDelMember(null, groupName, pSid);

                if (result != 0)
                {
                    throw new DirectoryException("NetLocalGroupDelMember failed", new Win32Exception(result));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pSid);
            }
        }


        public static IList<SecurityIdentifier> GetLocalGroupMembers(string groupName)
        {
            int result;

            List<SecurityIdentifier> list = new List<SecurityIdentifier>();

            do
            {
                int entriesRead;
                int totalEntries;
                IntPtr resume = IntPtr.Zero;
                IntPtr pLocalGroupMemberInfo = IntPtr.Zero;

                try
                {
                    result = NetLocalGroupGetMembers(null, groupName, 0, out pLocalGroupMemberInfo, -1, out entriesRead, out totalEntries, resume);

                    if (result != 0 && result != InsufficientBuffer)
                    {
                        throw new DirectoryException("NetLocalGroupGetMembers returned an error", new Win32Exception(result));
                    }

                    IntPtr currentPosition = pLocalGroupMemberInfo;

                    for (int i = 0; i < entriesRead; i++)
                    {
                        var item = (LocalGroupMembersInfo0)Marshal.PtrToStructure<LocalGroupMembersInfo0>(currentPosition);
                        list.Add(new SecurityIdentifier(item.pSID));
                        currentPosition = IntPtr.Add(currentPosition, Marshal.SizeOf(typeof(LocalGroupMembersInfo0)));
                    }
                }
                finally
                {
                    if (pLocalGroupMemberInfo != IntPtr.Zero)
                    {
                        NetApiBufferFree(pLocalGroupMemberInfo);
                    }
                }
            }
            while (result == ErrorMoreData);

            return list;
        }

        public static ServerInfo101 GetServerInfo(string server)
        {
            IntPtr pServerInfo = IntPtr.Zero;

            try
            {
                int result = NetServerGetInfo(server, 101, out pServerInfo);

                if (result != 0)
                {
                    throw new DirectoryException("NetServerGetInfo failed", new Win32Exception(result));
                }

                var info = Marshal.PtrToStructure<ServerInfo101>(pServerInfo);

                return info;
            }
            finally
            {
                if (pServerInfo != IntPtr.Zero)
                {
                    NetApiBufferFree(pServerInfo);
                }
            }
        }

        public static WorkstationInfo100 GetWorkstationInfo(string server)
        {
            IntPtr pServerInfo = IntPtr.Zero;

            try
            {
                int result = NetWkstaGetInfo(server, 100, out pServerInfo);

                if (result != 0)
                {
                    throw new DirectoryException("NetWkstaGetInfo failed", new Win32Exception(result));
                }

                var info = Marshal.PtrToStructure<WorkstationInfo100>(pServerInfo);

                return info;
            }
            finally
            {
                if (pServerInfo != IntPtr.Zero)
                {
                    NetApiBufferFree(pServerInfo);
                }
            }
        }

        public static string GetDn(SecurityIdentifier nameToFind)
        {
            return GetDn(nameToFind.ToString(), DsNameFormat.SecurityIdentifier);
        }

        public static string GetDn(string nameToFind)
        {
            return GetDn(nameToFind, DsNameFormat.Unknown);
        }

        public static string GetDn(string nameToFind, DsNameFormat nameFormat)
        {
            return GetDn(nameToFind, nameFormat, null);
        }

        public static string GetDn(string nameToFind, DsNameFormat nameFormat, string server)
        {
            var result = CrackNames(nameFormat, DsNameFormat.DistinguishedName, nameToFind, server);
            return result.Name;
        }

        public static DirectoryEntry GetDirectoryEntry(string nameToFind, DsNameFormat nameFormat)
        {
            return GetDirectoryEntry(nameToFind, nameFormat, null);
        }

        public static DirectoryEntry GetDirectoryEntry(string nameToFind, DsNameFormat nameFormat, string server)
        {
            var result = CrackNames(nameFormat, DsNameFormat.DistinguishedName, nameToFind, server);
            return new DirectoryEntry($"LDAP://{result.Domain}/{result.Name}");
        }

        public static DirectoryEntry GetDirectoryEntry(SecurityIdentifier nameToFind)
        {
            return GetDirectoryEntry(nameToFind.ToString(), DsNameFormat.SecurityIdentifier);
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

            if (principalSid == sidToCheck)
            {
                return true;
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

            using (AuthorizationContext context = new AuthorizationContext(principalSid, server))
            {
                return context.ContainsSid(sidToCheck);
            }
        }

        public static IEnumerable<SecurityIdentifier> GetTokenGroups(SecurityIdentifier principalSid, SecurityIdentifier requestContext = null)
        {
            if (principalSid == null)
            {
                throw new ArgumentNullException(nameof(principalSid));
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

            using (AuthorizationContext context = new AuthorizationContext(principalSid, server))
            {
                return context.GetTokenGroups().ToList(); // Force the enumeration now before the context goes out of scope
            }
        }

        public static string GetDomainControllerForDnsDomain(string dnsDomain, bool forceRediscovery = false)
        {
            IntPtr pdcInfo = IntPtr.Zero;

            try
            {
                int result = DsGetDcName(
                    null,
                    dnsDomain,
                    IntPtr.Zero,
                    null,
                    DsGetDcNameFlags.DS_DIRECTORY_SERVICE_8_REQUIRED |
                    DsGetDcNameFlags.DS_RETURN_DNS_NAME |
                    (forceRediscovery ? DsGetDcNameFlags.DS_FORCE_REDISCOVERY : 0),
                    out pdcInfo);

                if (result != 0)
                {
                    throw new DirectoryException("DsGetDcName failed", new Win32Exception(result));
                }

                DomainControllerInfo info = Marshal.PtrToStructure<DomainControllerInfo>(pdcInfo);

                return info.DomainControllerName.TrimStart('\\');
            }
            finally
            {
                if (pdcInfo != IntPtr.Zero)
                {
                    NetApiBufferFree(pdcInfo);
                }
            }
        }

        public static string GetDnsDomainNameFromSid(SecurityIdentifier sid)
        {
            var result = CrackNames(DsNameFormat.SecurityIdentifier, DsNameFormat.Nt4Name, sid.Value);
            return result.Domain;
        }

        public static DsNameResultItem CrackNames(DsNameFormat formatOffered, DsNameFormat formatDesired, string name, string dnsDomainName = null, int referralLevel = 0)
        {
            IntPtr hds = IntPtr.Zero;

            try
            {
                int result = NativeMethods.DsBind(null, dnsDomainName, out hds);
                if (result != 0)
                {
                    throw new DirectoryException("DsBind failed", new Win32Exception(result));
                }

                DsNameResultItem nameResult = NativeMethods.CrackNames(hds, DsNameFlags.DS_NAME_FLAG_TRUST_REFERRAL, formatOffered, formatDesired, name);

                switch (nameResult.Status)
                {
                    case DsNameError.None:
                        return nameResult;

                    case DsNameError.NoMapping:
                        throw new NameMappingException($"The object name {name} was found in the global catalog, but could not be mapped to a DN. DsCrackNames returned NO_MAPPING");

                    case DsNameError.TrustReferral:
                    case DsNameError.DomainOnly:
                        if (!string.IsNullOrWhiteSpace(nameResult.Domain))
                        {
                            if (referralLevel < NativeMethods.DirectoryReferralLimit)
                            {
                                return NativeMethods.CrackNames(formatOffered, formatDesired, name, nameResult.Domain, ++referralLevel);
                            }

                            throw new ReferralLimitExceededException("The referral limit exceeded the maximum configured value");
                        }

                        throw new ReferralFailedException($"A referral to the object name {name} was received from the global catalog, but no referral information was provided. DsNameError: {nameResult.Status}");

                    case DsNameError.NotFound:
                        throw new ObjectNotFoundException($"The object name {name} was not found in the global catalog");

                    case DsNameError.NotUnique:
                        throw new AmbiguousNameException($"There was more than one object with the name {name} in the global catalog");

                    case DsNameError.Resolving:
                        throw new NameMappingException($"The object name {name} was not able to be resolved in the global catalog. DsCrackNames returned RESOLVING");

                    case DsNameError.NoSyntacticalMapping:
                        throw new NameMappingException($"DsCrackNames unexpectedly returned DS_NAME_ERROR_NO_SYNTACTICAL_MAPPING for name {name}");

                    default:
                        throw new NameMappingException($"An unexpected status was returned from DsCrackNames {nameResult.Status}");
                }
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
                    throw new DirectoryException("DsCrackNames failed", new Win32Exception(result));
                }

                DsNameResult dsNameResult = (DsNameResult)Marshal.PtrToStructure(pDsNameResult, typeof(DsNameResult));

                if (dsNameResult.cItems == 0)
                {
                    throw new DirectoryException("DsCrackNames returned an unexpected result");
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
    }
}