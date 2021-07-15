using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using CsvHelper;
using Microsoft.Extensions.Logging;

namespace Lithnet.AccessManager.Server.UI.AuthorizationRuleImport
{
    public class ComputerPrincipalProviderCsv : IComputerPrincipalProviderCsv
    {
        private readonly IActiveDirectory directory;
        private readonly ILogger logger;

        private Dictionary<string, HashSet<string>> cache;

        public ComputerPrincipalProviderCsv(IActiveDirectory directory, ILogger<ComputerPrincipalProviderCsv> logger)
        {
            this.directory = directory;
            this.logger = logger;
            this.ComputerPropertiesToGet = new List<string>() { "samAccountName", "msDS-PrincipalName" };
        }

        public void ImportPrincipalMappings(string file, bool hasHeaderRow)
        {
            this.cache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            using (StreamReader reader = new StreamReader(file))
            {
                using (CsvReader csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    if (hasHeaderRow)
                    {
                        csv.Read();
                        csv.ReadHeader();
                    }

                    while (csv.Read())
                    {
                        string computerRaw = csv.GetField(0);
                        string principal = csv.GetField(1);

                        string key;

                        if (!computerRaw.Contains("\\"))
                        {
                            if (computerRaw.TryParseAsSid(out SecurityIdentifier sid))
                            {
                                try
                                {
                                    key = this.directory.TranslateName(sid.ToString(), AccessManager.Interop.DsNameFormat.SecurityIdentifier, AccessManager.Interop.DsNameFormat.Nt4Name).TrimEnd('$');
                                }
                                catch (Exception ex)
                                {
                                    this.logger.LogWarning(ex, "Computer {computer} was not found in the directory", computerRaw);
                                    continue;
                                }
                            }
                            else
                            {
                                if (this.directory.TryGetComputer(computerRaw, out IActiveDirectoryComputer c))
                                {
                                    key = c.MsDsPrincipalName.TrimEnd('$');
                                }
                                else
                                {
                                    this.logger.LogWarning("Computer {computer} was not found in the directory", computerRaw);
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            key = computerRaw;
                        }

                        HashSet<string> list;

                        if (!this.cache.ContainsKey(key))
                        {
                            list = new HashSet<string>();
                            this.cache.Add(key, list);
                        }
                        else
                        {
                            list = this.cache[key];
                        }

                        list.Add(principal);
                    }
                }
            }
        }

        public void ClearPrincipalMappings()
        {
            this.cache = null;
        }

        public List<SecurityIdentifier> GetPrincipalsForComputer(SearchResult computer, bool filterLocalAccounts)
        {
            if (this.cache == null)
            {
                throw new InvalidOperationException("The dictionary must first be initialized by calling ImportPrincipalMappings");
            }

            string computerName = computer.GetPropertyString("samAccountName").TrimEnd('$');
            string key = computer.GetPropertyString("msDS-PrincipalName").TrimEnd('$');

            if (!cache.ContainsKey(key))
            {
                throw new ObjectNotFoundException("The computer was not found in the import file");
            }

            List<SecurityIdentifier> sids = new List<SecurityIdentifier>();

            foreach (var member in cache[key])
            {
                if (filterLocalAccounts)
                {
                    if (member.StartsWith(computerName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                try
                {
                    if (member.TryParseAsSid(out SecurityIdentifier sid))
                    {
                        sids.Add(sid);
                    }
                    else
                    {
                        if (!directory.TryGetPrincipal(member, out IActiveDirectorySecurityPrincipal principal))
                        {
                            continue;
                        }

                        sids.Add(principal.Sid);
                    }

                }
                catch (Exception ex)
                {
                    this.logger.LogTrace(ex, "Unable to find principal {principal} in the directory", member);
                }
            }

            return sids;
        }

        public List<string> ComputerPropertiesToGet { get; }
    }
}
