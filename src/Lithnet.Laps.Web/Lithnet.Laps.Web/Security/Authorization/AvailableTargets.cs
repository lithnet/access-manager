using System.Collections.Generic;
using System.Linq;
using Lithnet.Laps.Web.Models;
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
            List<ITarget> matchingTargets = new List<ITarget>();

            foreach (TargetElement target in this.configSection.Targets.OfType<TargetElement>().OrderBy(t => t.Type == TargetType.Computer).ThenBy(t => t.Type == TargetType.Group))
            {
                if (target.Type == TargetType.Container)
                {
                    if (this.directory.IsComputerInOu(computer, target.Name))
                    {
                        this.logger.Trace($"Matched {computer.SamAccountName} to target OU {target.Name}");
                        matchingTargets.Add(target);
                    }
                }
                else if (target.Type == TargetType.Computer)
                {
                    IComputer p = this.directory.GetComputer(target.Name);

                    if (p == null)
                    {
                        this.logger.Trace($"Target computer {target.Name} was not found in the directory");
                        continue;
                    }

                    if (p.Equals(computer))
                    {
                        this.logger.Trace($"Matched {computer.SamAccountName} to target computer {target.Name}");
                        matchingTargets.Add(target);
                    }
                }
                else
                {
                    IGroup g = this.directory.GetGroup(target.Name);

                    if (g == null)
                    {
                        this.logger.Trace($"Target group {target.Name} was not found in the directory");
                        continue;
                    }

                    if (this.directory.IsComputerInGroup(computer, g))
                    {
                        this.logger.Trace($"Matched {computer.SamAccountName} to target group {target.Name}");
                        matchingTargets.Add(target);
                    }
                }
            }

            return matchingTargets.OrderBy(t => t.TargetType == TargetType.Computer).ThenBy(t => t.TargetType == TargetType.Group).FirstOrDefault();
        }
    }
}
