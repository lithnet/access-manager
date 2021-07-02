using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Api.Shared
{
    public class PasswordGetResponse
    {
        public string EncryptionCertificate { get; set; }

        public PasswordPolicy Policy { get; set; }
    }
}
