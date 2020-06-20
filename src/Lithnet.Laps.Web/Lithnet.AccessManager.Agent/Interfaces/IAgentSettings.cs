namespace Lithnet.AccessManager.Agent
{
    public interface IAgentSettings
    {
        int CheckInterval { get; }

        bool Enabled { get; }
    }
}