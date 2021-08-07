using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    public class CommandLineResult
    {
        public string StdOut { get; set; }

        public string StdErr { get; set; }

        public int ExitCode { get; set; }

        public bool Timeout { get; set; }
    }
}
