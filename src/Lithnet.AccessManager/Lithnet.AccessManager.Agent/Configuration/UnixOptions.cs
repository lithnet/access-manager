using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent.Configuration
{
    public class UnixOptions
    {
        public int DefaultCommandTimeoutSeconds { get; set; } = 5;

        public string DefaultShell { get; set; } = "/bin/sh";

        public string Username { get; set; } = "root";
    }
}
