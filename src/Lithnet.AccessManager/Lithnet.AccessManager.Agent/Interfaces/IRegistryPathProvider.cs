namespace Lithnet.AccessManager.Agent.Providers
{
    public interface IRegistryPathProvider
    {
        string PolicySettingsAgentPath { get; }
        string PolicySettingsPasswordPath { get; }
        string RegistrySettingsAgentPath { get; }
    }
}