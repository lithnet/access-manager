using System;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.InteropServices;

namespace Lithnet.Laps.Web.ActiveDirectory.Interop
{
    internal static class NativeMethods
    {
        private static int directoryReferralLimit = -1;

        private static int DirectoryReferralLimit
        {
            get
            {
                if (directoryReferralLimit < 0)
                {
                    if (!int.TryParse(ConfigurationManager.AppSettings["directory:referral-limit"] ?? "10", out directoryReferralLimit))
                    {
                        directoryReferralLimit = 10;
                    }
                }

                return directoryReferralLimit;
            }
        }

        [DllImport("Ntdsapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int DsBind(string domainControllerName, string dnsDomainName, out IntPtr hds);

        [DllImport("Ntdsapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int DsUnBind(IntPtr hds);

        [DllImport("ntdsapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int DsCrackNames(IntPtr hds, DsNameFlags flags, DsNameFormat formatOffered, DsNameFormat formatDesired, uint cNames, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPTStr, SizeParamIndex = 4)]
            string[] rpNames,
            out IntPtr ppResult);

        [DllImport("ntdsapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern void DsFreeNameResult(IntPtr pResult);

        public static string GetDnFromGc(string nameToFind, string dnsDomainName = null, int referralLevel = 0)
        {
            IntPtr hds = IntPtr.Zero;

            try
            {
                int result = DsBind(null, dnsDomainName, out hds);
                if (result != 0)
                {
                    throw new Win32Exception(result);
                }

                DsNameResultItem nameResult = CrackNames(hds, DsNameFlags.DS_NAME_FLAG_TRUST_REFERRAL, DsNameFormat.DS_UNKNOWN_NAME, DsNameFormat.DS_FQDN_1779_NAME, nameToFind);

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
                            if (referralLevel < DirectoryReferralLimit)
                            {
                              return GetDnFromGc(nameToFind, nameResult.Domain, ++referralLevel);
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
                    DsUnBind(hds);
                }
            }
        }

        private static DsNameResultItem CrackNames(IntPtr hds, DsNameFlags flags, DsNameFormat formatOffered, DsNameFormat formatDesired, string name)
        {
            var resultItems = CrackNames(hds, flags, formatOffered, formatDesired, new[] { name });
            return resultItems[0];
        }

        private static DsNameResultItem[] CrackNames(IntPtr hds, DsNameFlags flags, DsNameFormat formatOffered, DsNameFormat formatDesired, string[] namesToCrack)
        {
            IntPtr pDsNameResult = IntPtr.Zero;
            DsNameResultItem[] resultItems;

            try
            {
                uint namesToCrackCount = (uint)namesToCrack.Length;

                int result = DsCrackNames(hds, flags, formatOffered, formatDesired, namesToCrackCount, namesToCrack, out pDsNameResult);

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
                    DsFreeNameResult(pDsNameResult);
                }
            }

            return resultItems;
        }
    }
}