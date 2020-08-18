using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lithnet.AccessManager.Service.Models
{
    public class BitLockerRecoveryPasswordsModel
    {
        [Required]
        public string ComputerName { get; set; }

        public IList<BitLockerRecoveryPassword> Passwords { get; set; }
    }
}