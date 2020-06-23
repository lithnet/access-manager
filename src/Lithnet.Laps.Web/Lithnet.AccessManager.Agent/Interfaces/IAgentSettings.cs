namespace Lithnet.AccessManager.Agent
{
    public interface IAgentSettings
    {
        int Interval { get; }

        bool Enabled { get; }
    }
}