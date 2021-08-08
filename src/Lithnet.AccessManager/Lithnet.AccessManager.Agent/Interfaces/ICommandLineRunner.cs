using System;
using System.Collections.Generic;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface ICommandLineRunner
    {
        CommandLineResult ExecuteCommand(string cmd, params string[] args);

        CommandLineResult ExecuteCommand(string cmd, TimeSpan timeout, params string[] args);

    }
}