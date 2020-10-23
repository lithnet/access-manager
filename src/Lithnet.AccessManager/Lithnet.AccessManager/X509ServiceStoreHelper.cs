using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Vanara.PInvoke;

namespace Lithnet.AccessManager
{
    public static class X509ServiceStoreHelper
    {
        public static X509Store Open(string serviceName, OpenFlags openFlags)
        {
            Crypt32.CertStoreFlags storeFlags = Crypt32.CertStoreFlags.CERT_SYSTEM_STORE_SERVICES;

            return Open(storeFlags, openFlags, $"{serviceName}\\MY");
        }

        public static X509Store Open(OpenFlags flags)
        {
            Crypt32.CertStoreFlags storeFlags = Crypt32.CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_SERVICE;

            return Open(storeFlags, flags, "MY");
        }

        private static X509Store Open(Crypt32.CertStoreFlags storeFlags, OpenFlags openFlags, string storeName)
        {
            storeFlags |= openFlags.HasFlag(OpenFlags.MaxAllowed) ? Crypt32.CertStoreFlags.CERT_STORE_MAXIMUM_ALLOWED_FLAG : 0;
            storeFlags |= openFlags.HasFlag(OpenFlags.IncludeArchived) ? Crypt32.CertStoreFlags.CERT_STORE_ENUM_ARCHIVED_FLAG : 0;
            storeFlags |= openFlags.HasFlag(OpenFlags.OpenExistingOnly) ? Crypt32.CertStoreFlags.CERT_STORE_OPEN_EXISTING_FLAG : 0;
            storeFlags |= !openFlags.HasFlag(OpenFlags.ReadWrite) ? Crypt32.CertStoreFlags.CERT_STORE_READONLY_FLAG : 0;

            Crypt32.SafeHCERTSTORE pHandle = Crypt32.CertOpenStore(new Crypt32.SafeOID(10), Crypt32.CertEncodingType.X509_ASN_ENCODING, IntPtr.Zero, storeFlags, storeName);

            if (pHandle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var store = new X509Store(pHandle.DangerousGetHandle());
            pHandle.SetHandleAsInvalid(); // The X509Store object will take care of closing the handle
            return store;
        }
    }
}
