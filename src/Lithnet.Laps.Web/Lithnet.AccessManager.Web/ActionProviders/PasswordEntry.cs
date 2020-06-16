using System;

namespace Lithnet.AccessManager.Web
{
    public class PasswordEntry
    {
        public string Password { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}
