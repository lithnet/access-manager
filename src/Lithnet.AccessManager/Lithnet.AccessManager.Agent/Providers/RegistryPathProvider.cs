using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class RegistryPathProvider : IRegistryPathProvider
    {
        public string PolicyAgentPath { get; } = "HKEY_LOCAL_MACHINE\\Software\\Policies\\Lithnet\\Access Manager Agent";
        
        public string SettingsAgentPath { get; } = "HKEY_LOCAL_MACHINE\\Software\\Lithnet\\Access Manager Agent";

        public string PolicyPasswordPath { get; } = "HKEY_LOCAL_MACHINE\\Software\\Policies\\Lithnet\\Access Manager Agent\\Password";

        public string StatePath { get; } = "HKEY_LOCAL_MACHINE\\Software\\Lithnet\\Access Manager Agent\\State";
    }
}
