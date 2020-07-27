using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class EncryptedData
    {
        public bool IsEncrypted { get; set; }

        public string Data { get; set; }

        public string Salt { get; set; }
    }
}
