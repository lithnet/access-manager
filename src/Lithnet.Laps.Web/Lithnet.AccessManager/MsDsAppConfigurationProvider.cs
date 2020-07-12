using System.DirectoryServices;

namespace Lithnet.AccessManager
{
    public class MsDsAppConfigurationProvider : IAppDataProvider
    {
        internal static string[] PropertiesToGet = new string[] { "distinguishedName", "applicationName", "description", "msDS-ObjectReference", "msDS-Settings", "objectGuid", "objectClass", "msDS-DateTime" };

        internal const string ObjectClass = "msDS-App-Configuration";

        internal const string AttrApplicationName = "Lithnet Access Manager";

        internal const string AttrDescription = "Application configuration for Lithnet Access Manager";

        internal const string AttrCommonName = "LithnetAccessManagerConfig";

        internal static string Filter = $"(&(objectClass={ObjectClass})(applicationName={AttrApplicationName}))";

        public void DeleteAppData(IAppData settings)
        {
            settings.GetDirectoryEntry().DeleteTree();
        }

        public IAppData GetOrCreateAppData(IComputer computer)
        {
            if (!this.TryGetAppData(computer, out IAppData appData))
            {
                return Create(computer);
            }
            else
            {
                return appData;
            }
        }

        public bool TryGetAppData(IComputer computer, out IAppData appData)
        {
            return DirectoryExtensions.TryGet(() => this.GetAppData(computer), out appData);
        }

        public IAppData GetAppData(IComputer computer)
        {
            DirectorySearcher d = new DirectorySearcher
            {
                SearchRoot = computer.GetDirectoryEntry(),
                SearchScope = SearchScope.OneLevel,
                Filter = MsDsAppConfigurationProvider.Filter
            };

            foreach (string property in MsDsAppConfigurationProvider.PropertiesToGet)
            {
                d.PropertiesToLoad.Add(property);
            }

            var result = d.FindOne();

            if (result == null)
            {
                throw new ObjectNotFoundException();
            }

            return new MsDsAppConfiguration(result);
        }

        public IAppData Create(IComputer computer)
        {
            var parent = computer.GetDirectoryEntry();

            DirectoryEntry de = parent.Children.Add($"CN={MsDsAppConfigurationProvider.AttrCommonName}", MsDsAppConfigurationProvider.ObjectClass);
            de.Properties["applicationName"].Add(MsDsAppConfigurationProvider.AttrApplicationName);
            de.Properties["description"].Add(MsDsAppConfigurationProvider.AttrDescription);
            de.Properties["ntSecurityDescriptor"].Add(GetDefaultSecurityDescriptor());
            de.CommitChanges();

            return new MsDsAppConfiguration(de);
        }

        private static byte[] GetDefaultSecurityDescriptor()
        {
            ActiveDirectorySecurity gf = new ActiveDirectorySecurity();
            gf.SetSecurityDescriptorSddlForm("D:AI(A;;FA;;;CO)");
            return gf.GetSecurityDescriptorBinaryForm();
        }
    }
}