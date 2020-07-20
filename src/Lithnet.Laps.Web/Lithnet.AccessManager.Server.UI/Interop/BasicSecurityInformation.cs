using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    internal class BasicSecurityInformation : ISecurityInformation
    {
        private readonly string objectName;
        private readonly string serverName;
        private readonly string pageTitle;
        private readonly SiObjectInfoFlags flags;
        private readonly SiAccess[] accessRights;
        private readonly GenericMapping mapping;
        private const int S_OK = 0;
        
        public GenericSecurityDescriptor SecurityDescriptor { get; private set; }

        public BasicSecurityInformation(SiObjectInfoFlags flags, string objectName,
            List<SiAccess> accessRights, GenericSecurityDescriptor intitalSd, GenericMapping mapping,
            string serverName = null, string pageTitle = null)
        {
            this.flags = flags;
            this.objectName = objectName;
            this.serverName = serverName;
            this.pageTitle = pageTitle;
            this.accessRights = accessRights.ToArray();
            this.SecurityDescriptor = intitalSd;
            this.mapping = mapping;
        }

        public int GetObjectInformation(ref SiObjectInfo object_info)
        {
            object_info.pszObjectName = this.objectName;
            object_info.pszPageTitle = this.pageTitle;
            object_info.pszServerName = this.serverName;
            object_info.dwFlags = this.flags;
            object_info.hInstance = IntPtr.Zero;
                 
            return S_OK;
        }

        public int GetSecurity(SecurityInfos requestInformation, out IntPtr pSD, bool fDefault)
        {
            pSD = NativeMethods.GetSecurityDescriptor(this.SecurityDescriptor.GetSddlForm(AccessControlSections.All), requestInformation);

            return S_OK;
        }

        public int SetSecurity(SecurityInfos requestInformation, IntPtr securityDescriptor)
        {
            string s = NativeMethods.GetSecurityDescriptor(securityDescriptor, requestInformation);
            this.SecurityDescriptor = new RawSecurityDescriptor(s);
             
            return S_OK;
        }

        public int GetAccessRights(IntPtr guidObject, int dwFlags, out SiAccess[] access, out int accessCount, out int defaultAccess)
        {
            access = this.accessRights;
            accessCount = this.accessRights.Length;

            defaultAccess = 0;
            return S_OK;
        }

        public int MapGeneric(ref Guid guidObjectType, ref AceFlags aceFlags, ref int mask)
        {
            NativeMethods.MapGenericMask(ref mask, this.mapping);
            return S_OK;
        }

        public int GetInheritTypes(out IntPtr inheritTypes, out int inheritTypesCount)
        {
            inheritTypes = IntPtr.Zero;
            inheritTypesCount = 0;
            return S_OK;
        }

        public int PropertySheetPageCallback(IntPtr hwnd, PropertySheetCallbackMessage uMsg,
            SiPageType uPage)
        {
            return S_OK;
        }
    }
}