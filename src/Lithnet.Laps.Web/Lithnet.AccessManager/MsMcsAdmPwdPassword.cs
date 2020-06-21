using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager
{
    public class MsMcsAdmPwdPassword
    {
        public string Password { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}
