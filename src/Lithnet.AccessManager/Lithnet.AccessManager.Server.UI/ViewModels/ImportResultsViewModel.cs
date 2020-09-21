using System.Collections.Generic;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class ImportResultsViewModel : Screen
    {
        public ImportResultsViewModel()
        {
        }

        public SecurityDescriptorTargetsViewModel Targets { get; set; }

        public bool Merge { get; set; }

        public bool MergeOverwrite { get; set; }

        public bool HasDiscoveryErrors => DiscoveryErrors.Count > 0;

        public List<ComputerPrincipalMapping> DiscoveryErrors { get; set; }

        public string DiscoveryErrorCount => $"{DiscoveryErrors.Count} discovery error{(DiscoveryErrors.Count == 1 ? "" : "s")}";

        public string TargetCount => $"{Targets.ViewModels.Count} rule{(Targets.ViewModels.Count == 1 ? "" : "s")} found";

    }
}