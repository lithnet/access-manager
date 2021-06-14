using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Agent
{
    public interface IAgentCheckInProvider
    {
        Task CheckinIfRequired();
        Task<AgentCheckIn> GenerateCheckInData();
    }
}