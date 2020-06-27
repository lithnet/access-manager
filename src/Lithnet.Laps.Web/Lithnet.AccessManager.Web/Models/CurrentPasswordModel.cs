using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Lithnet.AccessManager.Web.Internal;

namespace Lithnet.AccessManager.Web.Models
{
    public class CurrentPasswordModel
    {
        [Required]
        public string ComputerName { get; set; }

        public string Password { get; set; }

        public DateTime? ValidUntil { get; set; }
    }
}