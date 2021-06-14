using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent.Configuration
{
    public class PasswordManagementOptions
    {
        public bool Enabled { get; set; } = false;

        public PasswordPolicyOptions PasswordPolicy { get; set; } = new PasswordPolicyOptions();

        public ActiveDirectoryOptions ActiveDirectorySettings { get; set; } = new ActiveDirectoryOptions();
    }
}
