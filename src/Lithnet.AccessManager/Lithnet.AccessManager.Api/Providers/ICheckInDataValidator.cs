using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Api.Providers
{
    public interface ICheckInDataValidator
    {
        void ValidateCheckInData(AgentCheckIn data);
    }
}