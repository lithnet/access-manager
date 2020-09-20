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
    }
}