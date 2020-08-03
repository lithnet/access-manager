using System;

namespace Lithnet.AccessManager
{
    public class PasswordEntry
    {
        public string Password { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public DateTime? Created { get; set; }
    }
}
