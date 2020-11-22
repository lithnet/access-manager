using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Lithnet.AccessManager.Server;

namespace Lithnet.AccessManager.Service.Models
{
    public class BitLockerRecoveryPasswordsModel
    {
        [Required]
        public string ComputerName { get; set; }

        public IList<BitLockerRecoveryPassword> Passwords { get; set; }
    }
}