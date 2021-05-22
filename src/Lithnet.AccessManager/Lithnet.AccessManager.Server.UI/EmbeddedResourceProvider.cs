using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace Lithnet.AccessManager.Server.UI
{
    internal static class EmbeddedResourceProvider
    {
        private static readonly EmbeddedFileProvider embeddedProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        public static string GetResourceString(string name)
        {
            return GetResourceString(name, "EmbeddedResources");
        }

        public static string GetResourceString(string name, string ns)
        {
            using (StreamReader reader = new StreamReader(embeddedProvider.GetFileInfo($"{ns}.{name}").CreateReadStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public static byte[] GetResourceBinary(string name)
        {
            return GetResourceBinary(name, "EmbeddedResources");
        }

        public static byte[] GetResourceBinary(string name, string ns)
        {
            using (Stream reader = embeddedProvider.GetFileInfo($"{ns}.{name}").CreateReadStream())
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

        public static Stream GetResourceStream(string name)
        {
            return GetResourceStream(name, "EmbeddedResources");
        }

        public static Stream GetResourceStream(string name, string ns)
        {
            return embeddedProvider.GetFileInfo($"{ns}.{name}").CreateReadStream();
        }
    }
}
