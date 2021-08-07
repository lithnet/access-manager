using Lithnet.AccessManager.Api.Shared;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Lithnet.AccessManager.Agent
{
    public static class CommandLineRunnerExtensions
    {
        public static void EnsureSuccess(this CommandLineResult result)
        {
            if (result.ExitCode != 0)
            {
                throw new CommandLineExecutionException($"The processed exited with error code {result.ExitCode}\nStdErr: {result.StdErr}\nStdOut: {result.StdOut}");

            }

            if (result.Timeout)
            {
                throw new CommandLineExecutionException($"The process did not exit in the time allotted and was terminated\nStdErr: {result.StdErr}\nStdOut: {result.StdOut}");
            }
        }
    }
}