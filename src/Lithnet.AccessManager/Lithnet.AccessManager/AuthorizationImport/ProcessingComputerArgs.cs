using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager
{
    public class ProcessingComputerArgs
    {
        public ProcessingComputerArgs()
        {
        }

        public ProcessingComputerArgs(string computerName)
        {
            this.ComputerName = computerName;
        }

        public string ComputerName { get; private set; }
    }
}
