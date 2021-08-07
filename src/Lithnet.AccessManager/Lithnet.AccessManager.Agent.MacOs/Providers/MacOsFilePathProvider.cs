using Microsoft.Extensions.Logging;
using System;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class MacOsFilePathProvider : IFilePathProvider
    {
        private static string confFilePath = "/Library/Application support/Lithnet/AccessManagerAgent/LithnetAccessManagerAgent.conf";
        private static string stateFilePath = "/Library/Application support/Lithnet/AccessManagerAgent/LithnetAccessManagerAgent.state";

        public string ConfFilePath => confFilePath;

        public string StateFilePath => stateFilePath;
    }
}
