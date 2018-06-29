using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Lithnet.Laps.Web.Models
{
    public class LapEntryModel
    {
        [Required]
        public string ComputerName { get; set; }

        public string Password { get; set; }

        public DateTime? ValidUntil { get; set; }

        public string FailureReason { get; set; }
    }
}