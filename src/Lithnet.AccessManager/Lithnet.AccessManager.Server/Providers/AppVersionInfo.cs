using System;

namespace Lithnet.AccessManager.Server
{
    public class AppVersionInfo
    {
        public AppVersionInfo()
        {
        }

        public VersionInfoStatus Status { get; set; }

        public Version AvailableVersion { get; set; }

        public Version CurrentVersion { get; set; }

        public string UpdateUrl { get; set; }

        public string ReleaseNotes { get; set; }

        public override string ToString()
        {
            return $"Status: {Status}, Available version: {AvailableVersion}, Update Url: {UpdateUrl}";
        }
    }
}
