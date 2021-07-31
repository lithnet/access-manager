namespace Lithnet.AccessManager.Agent.Providers
{
    public interface IRegistryPathProvider
    {
        string PolicyAgentPath { get; }
        
        string SettingsAgentPath { get; }

        string PolicyPasswordPath { get; }

        string StatePath { get; }
    }
}