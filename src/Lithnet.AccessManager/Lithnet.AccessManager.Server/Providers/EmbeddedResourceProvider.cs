using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Lithnet.AccessManager.Server
{
    internal static class EmbeddedResourceProvider
    {
        private static readonly EmbeddedFileProvider embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly(), "Lithnet.AccessManager.Server.EmbeddedResources");

        public static string GetResourceString(string name)
        {
            using (StreamReader reader = new StreamReader(embeddedProvider.GetFileInfo(name).CreateReadStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public static byte[] GetResourceBinary(string name)
        {
            using (Stream reader = embeddedProvider.GetFileInfo(name).CreateReadStream())
            {
                byte[] rawData;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    reader.CopyTo(memoryStream);
                    rawData = memoryStream.ToArray();
                }

                return rawData;
            }
        }
    }
}
