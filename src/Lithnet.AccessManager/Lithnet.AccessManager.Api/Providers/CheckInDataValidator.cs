using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;

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

            if (string.IsNullOrWhiteSpace(data.OperationSystemVersion))
            {
                throw new BadRequestException("The request did not provide a OS version");
            }
        }
    }
}
