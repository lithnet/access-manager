using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Management.Automation.Runspaces;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server
{
    public class BitLockerRecoveryPasswordProvider : IBitLockerRecoveryPasswordProvider
    {
        private readonly IDirectory directory;

        private readonly ILogger<BitLockerRecoveryPasswordProvider> logger;

        public BitLockerRecoveryPasswordProvider(IDirectory directory, ILogger<BitLockerRecoveryPasswordProvider> logger)
        {
            this.directory = directory;
            this.logger = logger;
        }

        public IList<BitLockerRecoveryPassword> GetBitLockerRecoveryPasswords(IComputer computer)
        {
            DirectorySearcher s = new DirectorySearcher(computer.DirectoryEntry)
            {
                SearchScope = SearchScope.OneLevel, 
                Filter = "(objectClass=msFVE-RecoveryInformation)"
            };

            s.PropertiesToLoad.Add("msFVE-RecoveryGuid");
            s.PropertiesToLoad.Add("cn");
            s.PropertiesToLoad.Add("msFVE-RecoveryPassword");
            s.PropertiesToLoad.Add("msFVE-VolumeGuid");

            List<BitLockerRecoveryPassword> results = new List<BitLockerRecoveryPassword>();

            foreach (var result in s.FindAll().OfType<SearchResult>())
            {
                string cn = result.GetPropertyString("cn");
                string rawdate = cn.Split('{')[0];
                DateTime.TryParse(rawdate, out DateTime date);

                results.Add(new BitLockerRecoveryPassword()
                {
                    RecoveryPassword = result.GetPropertyString("msFVE-RecoveryPassword"),
                    VolumeID = result.GetPropertyGuid("msFVE-VolumeGuid")?.ToString(),
                    PasswordID = result.GetPropertyGuid("msFVE-RecoveryGuid")?.ToString(),
                    Created = date
                });
            }

            return results;
        }
    }
}