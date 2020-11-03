using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Lithnet.AccessManager.Server;

namespace Lithnet.AccessManager.Service.Models
{
    public class PasswordHistoryModel
    {
        [Required]
        public string ComputerName { get; set; }

        public IList<PasswordEntry> PasswordHistory { get; set; }
    }
}