using Microsoft.Extensions.Logging;
using System;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class LinuxFilePathProvider : IFilePathProvider
    {
        private static string confFilePath = "/etc/LithnetAccessManagerAgent.conf";
        private static string stateFilePath = "/var/lib/LithnetAccessManagerAgent/LithnetAccessManagerAgent.state";

        public string ConfFilePath => confFilePath;

        public string StateFilePath => stateFilePath;
    }
}
