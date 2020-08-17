using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Service.App_LocalResources;

namespace Lithnet.AccessManager.Service.Models
{
    [Localizable(true)]
    public class LapRequestModel
    {
        [Required(ErrorMessageResourceType = typeof(UIMessages), ErrorMessageResourceName = "ComputerNameIsRequired")]
        public string ComputerName { get; set; }

        [MaxLength(4096, ErrorMessageResourceType = typeof(UIMessages), ErrorMessageResourceName = "ReasonTooLong")]
        public string UserRequestReason { get; set; }

        public AccessMask RequestType { get; set; } = AccessMask.LocalAdminPassword;

        public bool RequestLapsHistory { get; set; }

        public bool ShowReason { get; set; }

        public bool ReasonRequired { get; set; }

        public string FailureReason { get; set; }
    }
}