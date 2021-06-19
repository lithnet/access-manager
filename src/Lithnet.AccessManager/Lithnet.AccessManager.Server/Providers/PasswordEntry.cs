using System;

namespace Lithnet.AccessManager.Server
{
    public class PasswordEntry
    {
        public bool DecryptionFailed { get; set; }

        public string Password { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public DateTime? Created { get; set; }

        public string AccountName { get; set; }
    }
}
