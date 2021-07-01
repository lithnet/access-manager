using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class RegistryPathProvider : IRegistryPathProvider
    {
        public string PolicySettingsAgentPath { get; } = "HKEY_LOCAL_MACHINE\\Software\\Policies\\Lithnet\\Access Manager Agent";

        public string PolicySettingsPasswordPath { get; } = "HKEY_LOCAL_MACHINE\\Software\\Policies\\Lithnet\\Access Manager Agent\\Password";

        public string RegistrySettingsAgentPath { get; } = "HKEY_LOCAL_MACHINE\\Software\\Lithnet\\Access Manager Agent";
    }
}
