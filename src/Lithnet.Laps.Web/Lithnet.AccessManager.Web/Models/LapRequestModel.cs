using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Lithnet.AccessManager.Web.App_LocalResources;
using Lithnet.AccessManager.Web.Authorization;

namespace Lithnet.AccessManager.Web.Models
{
    [Localizable(true)]
    public class LapRequestModel
    {
        [Required(ErrorMessageResourceType = typeof(UIMessages), ErrorMessageResourceName = "ComputerNameIsRequired")]
        public string ComputerName { get; set; }

        [MaxLength(4096, ErrorMessageResourceType = typeof(UIMessages), ErrorMessageResourceName = "ReasonTooLong")]
        public string UserRequestReason { get; set; }

        public AccessMask RequestType { get; set; } = AccessMask.Laps;

        public bool RequestLapsHistory { get; set; }

        internal AccessMask RequestedAccess
        {
            get
            {
                if (this.RequestType == AccessMask.Laps)
                {
                    return this.RequestType | (this.RequestLapsHistory ? AccessMask.LapsHistory : 0);
                }
                else
                {
                    return this.RequestType;
                }
            }
        }

        public bool ShowReason { get; set; }

        public bool ReasonRequired { get; set; }

        public string FailureReason { get; set; }
    }
}