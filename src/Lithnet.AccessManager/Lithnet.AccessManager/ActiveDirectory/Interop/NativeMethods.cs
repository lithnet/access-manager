using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Interop
{
    internal static class NativeMethods
    {
        private static SecurityIdentifier localMachineSid;

        public static int DirectoryReferralLimit { get; set; } = 10;

        internal const int InsufficientBuffer = 122;

        private const int ErrorMoreData = 234;

        [DllImport("Ntdsapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int DsBind(string domainControllerName, string dnsDomainName, out IntPtr hds);

        [DllImport("Ntdsapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int DsUnBind(IntPtr hds);

        [DllImport("ntdsapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DsCrackNames(IntPtr hds, DsNameFlags flags, DsNameFormat formatOffered, DsNameFormat formatDesired, uint cNames, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPTStr, SizeParamIndex = 4)]
            string[] rpNames, out IntPtr ppResult);

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
        private static extern int NetLocalGroupGetMembers([MarshalAs(UnmanagedType.LPWStr)]
            string servername, [MarshalAs(UnmanagedType.LPWStr)]
            string localgroupname, int level, out IntPtr bufptr, int prefmaxlen, out int entriesread, out int totalentries, IntPtr resume_handle);

        [DllImport("netapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int NetLocalGroupAddMember(string server, string groupName, IntPtr sid);

        [DllImport("netapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int NetLocalGroupDelMember(string server, string groupName, IntPtr sid);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateWellKnownSid(WellKnownSidType wellKnownSidType, IntPtr domainSid, IntPtr pSid, ref int cbSid);

        [DllImport("Advapi32.dll", SetLastError = true, PreserveSig = true, CharSet = CharSet.Unicode)]
        private static extern int LsaQueryInformationPolicy(IntPtr pPolicyHandle, PolicyInformationClass informationClass, out IntPtr pData);

        [DllImport("advapi32.dll", SetLastError = true, PreserveSig = true, CharSet = CharSet.Unicode)]
        private static extern int LsaOpenPolicy([In] in LsaUnicodeString server, [In] in LsaObjectAttributes objectAttributes, LsaAccessPolicy desiredAccess, out IntPtr pPolicyHandle);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int LsaClose(IntPtr hPolicy);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int LsaNtStatusToWinError(int status);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int LsaFreeMemory(IntPtr buffer);

        [DllImport("netapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int DsGetSiteName(string computerName, out IntPtr siteName);

        private const int ERROR_SUCCESS = 0;

        public static string GetComputerSiteName(string computerName)
        {
            IntPtr pSiteName = IntPtr.Zero;

            try
            {
                int result = DsGetSiteName(computerName, out pSiteName);

                if (result != 0)
                {
                    throw new Win32Exception(result);
                }

                return Marshal.PtrToStringUni(pSiteName);
            }
            finally
            {
                if (pSiteName != IntPtr.Zero)
                {
                    NetApiBufferFree(pSiteName);
                }
            }
        }

        public static SecurityIdentifier GetLocalMachineAuthoritySid()
        {
            if (localMachineSid != null)
            {
                return localMachineSid;
            }



            localMachineSid = GetLocalMachineAuthoritySid(null);
            return localMachineSid;
        }

        public static SecurityIdentifier GetLocalMachineAuthoritySid(string server)
        {
            AdvApi32.LSA_OBJECT_ATTRIBUTES lsaObjectAttributes = new AdvApi32.LSA_OBJECT_ATTRIBUTES();

            var result = AdvApi32.LsaOpenPolicy(server, lsaObjectAttributes, AdvApi32.LsaPolicyRights.POLICY_VIEW_LOCAL_INFORMATION, out AdvApi32.SafeLSA_HANDLE pPolicyHandle);

            result.ThrowIfFailed("LsaOpenPolicy failed");

            result = AdvApi32.LsaQueryInformationPolicy(pPolicyHandle, AdvApi32.POLICY_INFORMATION_CLASS.PolicyAccountDomainInformation, out AdvApi32.SafeLsaMemoryHandle pPolicyData);

            result.ThrowIfFailed("LsaQueryInformationPolicy failed");

            var info = Marshal.PtrToStructure<PolicyAccountDomainInfo>(pPolicyData.DangerousGetHandle());

            return new SecurityIdentifier(info.DomainSid);
        }

        public static string GetBuiltInAdministratorsGroupName(string server)
        {
            var sid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

            byte[] sidbytes = new byte[sid.BinaryLength];
            sid.GetBinaryForm(sidbytes, 0);

            int accountNameSize = 0;
            int domainNameSize = 0;
            AdvApi32.SID_NAME_USE use;

            StringBuilder accountName = new StringBuilder(accountNameSize);
            StringBuilder domainName = new StringBuilder(domainNameSize);

            if (!AdvApi32.LookupAccountSid(server, sidbytes, accountName, ref accountNameSize, domainName, ref domainNameSize, out use))
            {
                int error = Marshal.GetLastWin32Error();
                if (error != Vanara.PInvoke.Win32Error.ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new Win32Exception(error);
                }
            }

            if (!AdvApi32.LookupAccountSid(server, sidbytes, accountName, ref accountNameSize, domainName, ref domainNameSize, out use))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }

            return accountName.ToString();
        }


        public static SecurityIdentifier CreateWellKnownSid(WellKnownSidType sidType)
        {
            return new SecurityIdentifier(sidType, GetLocalMachineAuthoritySid());
        }

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

        public static IList<SecurityIdentifier> GetLocalGroupMembers(string server, string groupName)
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
                    result = NetLocalGroupGetMembers(server, groupName, 0, out pLocalGroupMemberInfo, -1, out entriesRead, out totalEntries, resume);

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

        public static string GetDomainControllerForDnsDomain(string computerName, string dnsDomain, string siteName, DsGetDcNameFlags flags)
        {
            IntPtr pdcInfo = IntPtr.Zero;

            flags |= DsGetDcNameFlags.DS_RETURN_DNS_NAME;
            flags |= DsGetDcNameFlags.DS_WRITABLE_REQUIRED;

            if (!flags.HasFlag(DsGetDcNameFlags.DS_DIRECTORY_SERVICE_8_REQUIRED) &&
                !flags.HasFlag(DsGetDcNameFlags.DS_DIRECTORY_SERVICE_6_REQUIRED) &&
                !flags.HasFlag(DsGetDcNameFlags.DS_GC_SERVER_REQUIRED) &&
                !flags.HasFlag(DsGetDcNameFlags.DS_DIRECTORY_SERVICE_PREFERRED))
            {
                flags |= DsGetDcNameFlags.DS_DIRECTORY_SERVICE_REQUIRED;
            }

            try
            {
                int result = DsGetDcName(computerName, dnsDomain, IntPtr.Zero, siteName, flags, out pdcInfo);

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

        public static DsNameResultItem CrackNames(DsNameFormat formatOffered, DsNameFormat formatDesired, string name, string dc, string dnsDomainName, int referralLevel = 0)
        {
            IntPtr hds = IntPtr.Zero;

            try
            {
                int result = NativeMethods.DsBind(dc, dnsDomainName, out hds);
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
                                return NativeMethods.CrackNames(formatOffered, formatDesired, name, null, nameResult.Domain, ++referralLevel);
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