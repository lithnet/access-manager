using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Lithnet.Laps.Web.App_LocalResources;
using Lithnet.Laps.Web.Authorization;
using Lithnet.Laps.Web.Internal;

namespace Lithnet.Laps.Web.Models
{
    [Localizable(true)]
    public class LapRequestModel
    {
        [Required(ErrorMessageResourceType = typeof(UIMessages), ErrorMessageResourceName = "ComputerNameIsRequired")]
        public string ComputerName { get; set; }

        [MaxLength(4096, ErrorMessageResourceType = typeof(UIMessages), ErrorMessageResourceName = "ReasonTooLong")]
        public string UserRequestReason { get; set; }

        public AccessMask RequestType { get; set; } = AccessMask.Laps;

        public bool ShowReason { get; set; }

        public bool ReasonRequired { get; set; }

        public string FailureReason { get; set; }
    }
}