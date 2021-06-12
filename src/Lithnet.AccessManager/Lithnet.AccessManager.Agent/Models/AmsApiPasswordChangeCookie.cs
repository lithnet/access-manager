using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent.Models
{
    public class AmsApiPasswordChangeCookie
    {
        public string EncryptionThumbprint { get; set; }

        public string PasswordId { get; set; }
    }
}
