using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class EncryptedData
    {
        public string Data { get; set; }

        public string Salt { get; set; }

        public int Mode { get; set; } = 0;
    }
}
