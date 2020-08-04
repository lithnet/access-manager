using System;
using System.DirectoryServices.ActiveDirectory;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class JitDomainStatusViewModel : PropertyChangedBase
    {
        private readonly IDirectory directory;

        private readonly Forest forest;

        private readonly ILogger logger;

        public JitDomainStatusViewModel(IDirectory directory, JitDynamicGroupMapping mapping, Domain domain, ILogger<JitDomainStatusViewModel> logger)
        {
            this.directory = directory;
            this.Domain = domain;
            this.forest = domain.Forest;
            this.logger = logger;
            this.Mapping = mapping;
            this.DynamicGroupOU = mapping?.GroupOU;
            _ = this.RefreshStatus();
        }

        public Domain Domain { get; }

        public JitDynamicGroupMapping Mapping { get; set; }

        public string ForestName => this.forest.Name;

        public string DomainName => this.Domain.Name;

        public string DynamicGroupOU { get; set; }

        public string ForestFunctionalLevel
        {
            get
            {
                return this.forest.ForestModeLevel switch
                {
                    0 => "Windows 2000 Server",
                    1 => "Windows Server 2003 Mixed Mode",
                    2 => "Windows Server 2003",
                    3 => "Windows Server 2008",
                    4 => "Windows Server 2008 R2",
                    5 => "Windows Server 2012",
                    6 => "Windows Server 2012 R2",
                    var e when e >= 7 => "Windows Server 2016 or higher",
                    _ => "Unknown forest functional level"
                };
            }
        }

        public string DomainFunctionalLevel
        {
            get
            {
                return this.Domain.DomainModeLevel switch
                {
                    0 => "Windows 2000 Server",
                    1 => "Windows Server 2003 Mixed Mode",
                    2 => "Windows Server 2003",
                    3 => "Windows Server 2008",
                    4 => "Windows Server 2008 R2",
                    5 => "Windows Server 2012",
                    6 => "Windows Server 2012 R2",
                    var e when e >= 7 => "Windows Server 2016 or higher",
                    _ => "Unknown domain functional level"
                };
            }
        }

        public string PamStatus { get; set; }

        public bool IsPamSupported { get; set; }

        public bool IsPamEnabled { get; set; }

        public bool IsPamNotSupported { get; set; }

        public string JitType { get; set; }

        public async Task RefreshStatus()
        {
            await Task.Run(() =>
            {
                try
                {
#if DEBUG
                    if (Mapping?.OverrideMode == 1)
                    {
                        this.PamStatus = "Enabled";
                        this.IsPamEnabled = true;
                        this.JitType = "Time-based membership";
                        this.DynamicGroupOU = null;
                        return;
                    }
                    else if (Mapping?.OverrideMode == 2)
                    {
                        this.PamStatus = "Not supported";
                        this.IsPamNotSupported = true;
                        this.JitType = "Dynamic group";
                        return;
                    }
                    else if (Mapping?.OverrideMode == 3)
                    {
                        this.PamStatus = "Available, but not enabled";
                        this.JitType = "Dynamic group";
                        this.IsPamSupported = true;
                        return;
                    }
#endif

                    if (directory.IsPamFeatureEnabled(Domain.Name))
                    {
                        this.PamStatus = "Enabled";
                        this.JitType = "Time-based membership";
                        this.IsPamEnabled = true;
                        this.DynamicGroupOU = null;
                    }
                    else if (Domain.DomainModeLevel >= 7)
                    {
                        this.PamStatus = "Available, but not enabled";
                        this.JitType = "Dynamic group";
                        this.IsPamSupported = true;
                    }
                    else
                    {
                        this.PamStatus = "Not supported";
                        this.JitType = "Dynamic group";
                        this.IsPamNotSupported = true;
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(EventIDs.UIGenericError, ex, "Could not determine JIT capability");
                    this.PamStatus = "Error determining functional levels";
                }
            }).ConfigureAwait(false);
        }
    }
}
