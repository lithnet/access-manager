using System;
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

            foreach (TargetElement target in this.configSection.Targets.OfType<TargetElement>().OrderBy(t => (int)t.Type))
            {
                try
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
                        IComputer p;
                        try
                        {
                            p = this.directory.GetComputer(target.Name);
                        }
                        catch (NotFoundException ex)
                        {
                            this.logger.Trace(ex, $"Target computer {target.Name} was not found in the directory");
                            continue;
                        }

                        if (p.Sid == computer.Sid)
                        {
                            this.logger.Trace($"Matched {computer.SamAccountName} to target computer {target.Name}");
                            matchingTargets.Add(target);
                        }
                    }
                    else
                    {
                        IGroup g;
                        try
                        {
                            g = this.directory.GetGroup(target.Name);
                        }
                        catch (NotFoundException ex)
                        {
                            this.logger.Trace(ex, $"Target group {target.Name} was not found in the directory");
                            continue;
                        }

                        if (this.directory.IsComputerInGroup(computer, g))
                        {
                            this.logger.Trace($"Matched {computer.SamAccountName} to target group {target.Name}");
                            matchingTargets.Add(target);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, $"An error occurred processing the target {target.Type}:{target.Name}");
                }
            }

            return matchingTargets.FirstOrDefault();
        }
    }
}
