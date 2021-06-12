using System;

namespace Lithnet.AccessManager.Api.Shared
{
    public class PasswordUpdateRequest
    {
        public string PasswordData { get; set; }

        public string AccountName { get; set; }

        public DateTime ExpiryDate { get; set; }
    }
}