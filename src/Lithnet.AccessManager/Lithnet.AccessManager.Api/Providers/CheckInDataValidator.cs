using Lithnet.AccessManager.Api.Shared;
using System;

namespace Lithnet.AccessManager.Api.Providers
{
    public class CheckInDataValidator : ICheckInDataValidator
    {
        public void ValidateCheckInData(AgentCheckIn data)
        {
            if (string.IsNullOrWhiteSpace(data.AgentVersion))
            {
                throw new BadRequestException("The request did not provide an agent version");
            }

            if (string.IsNullOrWhiteSpace(data.DnsName))
            {
                throw new BadRequestException("The request did not provide a DNS host name");
            }

            if (string.IsNullOrWhiteSpace(data.Hostname))
            {
                throw new BadRequestException("The request did not provide a host name");
            }

            if (string.IsNullOrWhiteSpace(data.OperatingSystem))
            {
                throw new BadRequestException("The request did not provide a OS family");
            }

            if (string.IsNullOrWhiteSpace(data.OperatingSystemVersion))
            {
                throw new BadRequestException("The request did not provide a OS version");
            }

            if (string.Equals(data.Hostname, "localhost", StringComparison.OrdinalIgnoreCase) || string.Equals(data.DnsName, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("The hostname provided by the agent was set to 'localhost'");
            }
        }
    }
}
