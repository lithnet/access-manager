using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Windows;
using Lithnet.AccessManager.Interop;
using Vanara.PInvoke;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    internal static class NativeMethods
    {
        private const int MAX_PATH = 256;

        private const int S_FALSE = 1;

        private const int S_OK = 0;

        private const uint SERVICE_NO_CHANGE = 0xffffffff;

        private const string CFSTR_DSOP_DS_SELECTION_LIST = "CFSTR_DSOP_DS_SELECTION_LIST";

        private const int CRYPTUI_WIZ_IMPORT_ALLOW_CERT = 0x00020000;

        private const int CRYPTUI_WIZ_IMPORT_NO_CHANGE_DEST_STORE = 0x00010000;

        private const int CRYPTUI_WIZ_IMPORT_TO_LOCALMACHINE = 0x00100000;


        [DllImport("AclUI.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EditSecurity([Optional] IntPtr hwndOwner, ISecurityInformation psi);

        [DllImport("AdvApi32.dll", ExactSpelling = true)]
        public static extern void MapGenericMask(ref int AccessMask, in GenericMapping GenericMapping);

        [DllImport("AdvApi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ConvertSecurityDescriptorToStringSecurityDescriptor(IntPtr pSD, int sdRevision, SecurityInfos securityInfo, out IntPtr securityDescriptor, out uint length);

        [DllImport("AdvApi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string securityDescriptor, int sdRevision, out IntPtr pSD, out uint size);

        [DllImport("dsuiext.dll", CharSet = CharSet.Unicode)]
        private static extern DsBrowseResult DsBrowseForContainer(IntPtr pInfo);

        [DllImport("ole32.dll")]
        private static extern void ReleaseStgMedium([In] ref STGMEDIUM stgmedium);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr ptr);

        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr ptr);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool ChangeServiceConfig(IntPtr hService, uint nServiceType, uint nStartType, uint nErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string pDependencies, string lpServiceStartName, string lpPassword, string lpDisplayName);

        [DllImport("cryptui.dll", SetLastError = true)]
        private static extern bool CryptUIWizImport(int dwFlags, IntPtr hwndParent, [MarshalAs(UnmanagedType.LPWStr)] string pwszWizardTitle, IntPtr pImportSrc, IntPtr hDestCertStore);

        public static string ShowContainerDialog(IntPtr hwnd, string dialogTitle = null, string treeViewTitle = null, string basePath = null, string initialPath = null, AdsFormat pathFormat = AdsFormat.X500Dn)
        {
            IntPtr pInfo = IntPtr.Zero;

            try
            {
                DSBrowseInfo info = new DSBrowseInfo();

                info.StructSize = Marshal.SizeOf(info);
                info.DialogCaption = dialogTitle;
                info.TreeViewTitle = treeViewTitle;
                info.DialogOwner = hwnd;

                if (string.IsNullOrWhiteSpace(basePath))
                {
                    info.Flags |= DsBrowseInfoFlags.EntireDirectory;
                }
                else
                {
                    info.Flags |= DsBrowseInfoFlags.EntireDirectory;
                    info.RootPath = basePath;
                }

                if (!string.IsNullOrWhiteSpace(initialPath))
                {
                    info.Flags |= DsBrowseInfoFlags.ExpandOnOpen;
                    info.Path = initialPath.PadRight(MAX_PATH - initialPath.Length, '\0');
                }
                else
                {
                    info.Path = new string(new char[MAX_PATH]);
                }

                info.Flags |= DsBrowseInfoFlags.ReturnFormat | DsBrowseInfoFlags.ReturnObjectClass;
                info.ReturnFormat = pathFormat;
                info.ObjectClass = new string(new char[MAX_PATH]);
                info.ObjectClassSize = MAX_PATH;
                info.PathSize = info.Path.Length;

                pInfo = Marshal.AllocHGlobal(Marshal.SizeOf<DSBrowseInfo>());
                Marshal.StructureToPtr(info, pInfo, false);

                DsBrowseResult status = DsBrowseForContainer(pInfo);

                if (status == DsBrowseResult.Ok)
                {
                    DSBrowseInfo result = Marshal.PtrToStructure<DSBrowseInfo>(pInfo);
                    return result.Path;
                }

                return null;
            }
            finally
            {
                if (pInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pInfo);
                }
            }
        }

        public static IEnumerable<DsopResult> ShowObjectPickerDialog(IntPtr hwnd, string targetComputer, DsopDialogInitializationOptions options, DsopScopeInitInfo scope, params string[] attributesToGet)
        {
            return ShowObjectPickerDialog(hwnd, targetComputer, options, new List<DsopScopeInitInfo> { scope }, attributesToGet);
        }

        public static IEnumerable<DsopResult> ShowObjectPickerDialog(IntPtr hwnd, string targetComputer, DsopDialogInitializationOptions options, IList<DsopScopeInitInfo> scopes, params string[] attributesToGet)
        {
            IDsObjectPicker idsObjectPicker = (IDsObjectPicker)new DSObjectPicker();

            try
            {
                using LpStructArrayMarshaller<DsopScopeInitInfo> scopeInitInfoArray = CreateScopes(scopes);
                using LpStringArrayConverter attributes = new LpStringArrayConverter(attributesToGet);
                DsopDialogInitializationInfo initInfo = CreateInitInfo(scopeInitInfoArray.Ptr, targetComputer, scopeInitInfoArray.Count, options, attributes.Ptr, attributes.Count);

                int hresult = idsObjectPicker.Initialize(ref initInfo);

                if (hresult != S_OK)
                {
                    throw new COMException("Directory object picker initialization failed", hresult);
                }

                hresult = idsObjectPicker.InvokeDialog(hwnd, out IDataObject data);

                if (hresult == S_FALSE)
                {
                    return new List<DsopResult>();
                }

                if (hresult != S_OK)
                {
                    throw new COMException("Directory object picker dialog activation failed", hresult);
                }

                return GetResultsFromDataObject(data, attributesToGet);
            }
            finally
            {
                if (idsObjectPicker != null)
                {
                    Marshal.ReleaseComObject(idsObjectPicker);
                }
            }
        }

        public static void ChangeServiceCredentials(string serviceName, string username, string password)
        {
            ServiceController controller = new ServiceController(serviceName);

            if (!ChangeServiceConfig(controller.ServiceHandle.DangerousGetHandle(), SERVICE_NO_CHANGE,
                                     SERVICE_NO_CHANGE, SERVICE_NO_CHANGE, null, null, IntPtr.Zero, null, username, password, null))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public static X509Certificate2 ShowCertificateImportDialog(IntPtr hwnd, string title, StoreLocation location, StoreName name)
        {
            using (X509Store store = new X509Store(name, StoreLocation.LocalMachine, OpenFlags.ReadWrite))
            {
                return ShowCertificateImportDialog(hwnd, title, store);
            }
        }

        public static X509Certificate2 ShowCertificateImportDialog(IntPtr hwnd, string title, X509Store store)
        {
            List<string> thumbprints = store.Certificates.OfType<X509Certificate2>().Select(t => t.Thumbprint).ToList();

            if (!CryptUIWizImport(CRYPTUI_WIZ_IMPORT_ALLOW_CERT | CRYPTUI_WIZ_IMPORT_NO_CHANGE_DEST_STORE | ((store.Location == StoreLocation.LocalMachine) ? CRYPTUI_WIZ_IMPORT_TO_LOCALMACHINE : 0),
                                  hwnd, title, IntPtr.Zero, store.StoreHandle))
            {
                int result = Marshal.GetLastWin32Error();
                throw new Win32Exception(result);
            }

            var newCertificateList = store.Certificates.OfType<X509Certificate2>().ToList();

            var newItems = newCertificateList.Where(t => thumbprints.All(u => u != t.Thumbprint));

            foreach (var newItem in newItems)
            {
                if (newItem.HasPrivateKey)
                {
                    return newItem;
                }
            }

            return null;
        }

        public static void ShowCertificateExportDialog(IntPtr hwnd, string title, X509Certificate2 certificate)
        {
            CryptUI.CRYPTUI_WIZ_EXPORT_INFO exportInfo = new CryptUI.CRYPTUI_WIZ_EXPORT_INFO
            {
                dwSubjectChoice = CryptUI.CryptUIWizExportType.CRYPTUI_WIZ_EXPORT_CERT_CONTEXT,
                Subject =
                {
                    pCertContext = certificate.Handle
                },
            };

            CryptUI.CryptUIWizFlags flags = 0;

            if (certificate.HasPrivateKey)
            {
                flags = CryptUI.CryptUIWizFlags.CRYPTUI_WIZ_EXPORT_PRIVATE_KEY;
            }

            CryptUI.CryptUIWizExport(flags, hwnd, title, exportInfo);
        }

        public static string GetSecurityDescriptor(IntPtr pSD, SecurityInfos requestedInformation)
        {
            IntPtr pString = IntPtr.Zero;

            try
            {
                if (!ConvertSecurityDescriptorToStringSecurityDescriptor(pSD, 1, requestedInformation, out pString, out uint len))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                return Marshal.PtrToStringUni(pString) ?? string.Empty;
            }
            finally
            {
                if (pString != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pString);
                }
            }
        }

        public static IntPtr GetSecurityDescriptor(string securityDescriptor, SecurityInfos requestedInformation)
        {
            if (!ConvertStringSecurityDescriptorToSecurityDescriptor(securityDescriptor, 1, out IntPtr pSD, out uint size))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return pSD;
        }

        private static DsopDialogInitializationInfo CreateInitInfo(IntPtr pScopeInitInfo, string targetComputer, int scopeCount, DsopDialogInitializationOptions options, IntPtr attrributesToGet, int attributesToGetCount)
        {
            var initInfo = new DsopDialogInitializationInfo
            {
                Size = Marshal.SizeOf<DsopDialogInitializationInfo>(),
                TargetComputer = targetComputer,
                ScopeInfoCount = scopeCount,
                ScopeInfo = pScopeInitInfo,
                Options = options
            };

            initInfo.AttributesToFetchCount = attributesToGetCount;
            initInfo.AttributesToFetch = attrributesToGet;

            return initInfo;
        }

        private static LpStructArrayMarshaller<DsopScopeInitInfo> CreateScopes(IList<DsopScopeInitInfo> scopes)
        {
            for (int i = 0; i < scopes.Count; i++)
            {
                var s = scopes[i];
                s.Size = Marshal.SizeOf<DsopScopeInitInfo>();
            }

            return new LpStructArrayMarshaller<DsopScopeInitInfo>(scopes);
        }

        private static IEnumerable<DsopResult> GetResultsFromDataObject(IDataObject data, string[] requestedAttributes)
        {
            IntPtr pSelectionList;

            STGMEDIUM storageMedium = new STGMEDIUM
            {
                tymed = TYMED.TYMED_HGLOBAL,
                unionmember = IntPtr.Zero,
                pUnkForRelease = IntPtr.Zero
            };

            FORMATETC formatEtc = new FORMATETC
            {
                cfFormat = (short)DataFormats.GetDataFormat(CFSTR_DSOP_DS_SELECTION_LIST).Id,
                ptd = IntPtr.Zero,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = TYMED.TYMED_HGLOBAL
            };

            int result = data.GetData(ref formatEtc, ref storageMedium);

            if (result != S_OK)
            {
                throw new COMException("GetData failed", result);
            }

            pSelectionList = GlobalLock(storageMedium.unionmember);
            if (pSelectionList == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                var selectionList = Marshal.PtrToStructure<DsSelectionList>(pSelectionList);
                IntPtr current = new IntPtr(pSelectionList.ToInt64() + Marshal.SizeOf(selectionList.FetchedAttributeCount.GetType()) + Marshal.SizeOf(selectionList.Count.GetType()));

                if (selectionList.Count > 0)
                {
                    for (int i = 0; i < selectionList.Count; i++)
                    {
                        var f = Marshal.PtrToStructure<DsSelection>(current);
                        yield return new DsopResult(f, requestedAttributes, (int)selectionList.FetchedAttributeCount);
                        current = new IntPtr(current.ToInt64() + Marshal.SizeOf<DsSelection>());
                    }
                }
            }
            finally
            {
                if (pSelectionList != IntPtr.Zero)
                {
                    GlobalUnlock(pSelectionList);
                }

                ReleaseStgMedium(ref storageMedium);
            }
        }
    }
}
