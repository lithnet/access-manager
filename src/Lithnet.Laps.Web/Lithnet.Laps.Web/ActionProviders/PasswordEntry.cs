using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web
{
    public class PasswordEntry
    {
        public string Password { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}
