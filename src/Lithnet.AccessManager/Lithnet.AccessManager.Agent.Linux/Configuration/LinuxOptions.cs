using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent.Linux.Configuration
{
    public class LinuxOptions
    {
        public bool? DisableChpasswd { get; set; }

        public string ChpasswdPath { get; set; }

        public string ChpasswdArgs { get; set; }

        public string PasswdPath { get; set; }

        public string PasswdArgs { get; set; }
    }
}
