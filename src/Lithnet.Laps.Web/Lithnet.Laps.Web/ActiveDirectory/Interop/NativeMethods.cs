using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Lithnet.Laps.Web.ActiveDirectory.Interop
{
    public static class NativeMethods
    {
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

        public static string GetDnFromGc(string name
            )
        {
            IntPtr hds = IntPtr.Zero;

            try
            {
                int result = DsBind(null, null, out hds);
                if (result != 0)
                {
                    throw new Win32Exception(result);
                }

                DsNameResultItem nameResult = CrackNames(hds, DsNameFlags.DS_NAME_FLAG_TRUST_REFERRAL, DsNameFormat.DS_UNKNOWN_NAME, DsNameFormat.DS_FQDN_1779_NAME, name);

                string x = nameResult.Name;

                return x;
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
            IntPtr pResult = IntPtr.Zero;
            uint cNames = 1;
            string[] rpNames = new[] { name };

            int rc = DsCrackNames(
                hds,
                flags,
                formatOffered,
                formatDesired,
                cNames,
                rpNames,
                out pResult
            );

            if (rc != 0)
            {
                throw new Win32Exception(rc);
            }

            DsNameResultItem[] dnri = null;

            try
            {
                DsNameResult dnr = (DsNameResult)Marshal.PtrToStructure(pResult, typeof(DsNameResult));

                if (dnr.cItems == 0)
                {
                    return default(DsNameResultItem);
                }

                //define the array with size to match				
                dnri = new DsNameResultItem[dnr.cItems];

                //point to our current DS_NAME_RESULT_ITEM structure
                IntPtr pidx = dnr.rItems;

                for (int idx = 0; idx < dnr.cItems; idx++)
                {
                    //marshall back the structure
                    dnri[idx] = (DsNameResultItem)Marshal.PtrToStructure(pidx, typeof(DsNameResultItem));
                    //update the current pointer idx to next structure
                    pidx = (IntPtr)(pidx.ToInt32() + Marshal.SizeOf(dnri[idx]));
                }
            }
            finally
            {
                DsFreeNameResult(pResult);
            }

            return dnri[0];
        }
    }
}