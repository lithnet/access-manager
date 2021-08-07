using System;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface ICommandLineRunner
    {
        CommandLineResult ExecuteCommand(string cmd, string args);
        
        CommandLineResult ExecuteCommand(string cmd, string args, TimeSpan timeout);
        
        CommandLineResult ExecuteCommandWithDefaultShell(string cmd);
        
        CommandLineResult ExecuteCommandWithDefaultShell(string cmd, TimeSpan timeout);
        
        CommandLineResult ExecuteCommandWithDefaultShell(string cmd, string args, TimeSpan timeout);
        
        CommandLineResult ExecuteCommandWithDefaultShell(string cmd, string args);

        bool TryExecuteCommandWithShell(string cmd, out CommandLineResult result);
    }
}