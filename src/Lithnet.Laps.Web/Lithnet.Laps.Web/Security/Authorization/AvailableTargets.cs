using System.Collections.Generic;
using System.Linq;
using Lithnet.Laps.Web.Models;
using Lithnet.Laps.Web.Security.Authorization.ConfigurationFile;
using NLog;

namespace Lithnet.Laps.Web.Security.Authorization
{
    public class AvailableTargets: IAvailableTargets
    {
        private readonly ILapsConfig configSection;
        private readonly IDirectory directory;
        private readonly ILogger logger;

        public AvailableTargets(ILapsConfig configSection, IDirectory directory, ILogger logger)
        {
            this.configSection = configSection;
            this.directory = directory;
            this.logger = logger;
        }

        public ITarget GetMatchingTargetOrNull(IComputer computer)
        {
            var matchingTargets = new List<ITarget>();

            foreach (TargetElement target in configSection.Targets.OfType<TargetElement>().OrderBy(t => t.Type == TargetType.Computer).ThenBy(t => t.Type == TargetType.Group))
            {
                if (target.Type == TargetType.Container)
                {
                    if (directory.IsComputerInOu(computer, target.Name))
                    {
                        logger.Trace($"Matched {computer.SamAccountName} to target OU {target.Name}");
                        matchingTargets.Add(target);
                    }

                    continue;
                }
                else if (target.Type == TargetType.Computer)
                {
                    var p = directory.GetComputer(target.Name);

                    if (p == null)
                    {
                        logger.Trace($"Target computer {target.Name} was not found in the directory");
                        continue;
                    }

                    if (p.Equals(computer))
                    {
                        logger.Trace($"Matched {computer.SamAccountName} to target computer {target.Name}");
                        return target;
                    }
                }
                else
                {
                    var g = directory.GetGroup(target.Name);

                    if (g == null)
                    {
                        logger.Trace($"Target group {target.Name} was not found in the directory");
                        continue;
                    }

                    if (directory.IsComputerInGroup(computer, g))
                    {
                        logger.Trace($"Matched {computer.SamAccountName} to target group {target.Name}");
                        matchingTargets.Add(target);
                    }
                }
            }

            return matchingTargets.OrderBy(t => t.TargetType == TargetType.Computer).ThenBy(t => t.TargetType == TargetType.Group).FirstOrDefault();
        }
    }
}
