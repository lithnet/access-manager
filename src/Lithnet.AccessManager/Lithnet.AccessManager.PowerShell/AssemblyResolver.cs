using System;
using System.IO;
using System.Management.Automation;
using System.Reflection;

namespace Lithnet.AccessManager.PowerShell
{
    public class AssemblyResolver : IModuleAssemblyInitializer, IModuleAssemblyCleanup
    {
        private static readonly string basePath = Path.GetFullPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        public void OnImport()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assyPath = Path.Combine(basePath, $"{args.Name}.dll");

            if (File.Exists(assyPath))
            {
                return Assembly.Load(assyPath);
            }

            return null;
        }

        public void OnRemove(PSModuleInfo psModuleInfo)
        {
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
        }
    }
}
