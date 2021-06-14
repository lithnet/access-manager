using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    public static class OptionExtensions
    {
        public static List<string> BuildValidAudiences(this ApiOptions apiOptions, string audiencePath = null)
        {
            List<string> validAudiences = new List<string>();

            foreach (var audience in apiOptions.ValidAudiences)
            {
                validAudiences.Add($"https://{audience}/api/v1.0/{audiencePath}");
            }

            return validAudiences;
        }
    }
}
