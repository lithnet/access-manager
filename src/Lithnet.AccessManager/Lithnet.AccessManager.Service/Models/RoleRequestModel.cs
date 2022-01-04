using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Service.App_LocalResources;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lithnet.AccessManager.Service.Models
{
    [Localizable(true)]
    public class RoleRequestModel
    {
        [MaxLength(4096, ErrorMessageResourceType = typeof(UIMessages), ErrorMessageResourceName = "ReasonTooLong")]
        public string UserRequestReason { get; set; }

        public string FailureReason { get; set; }

        [Required(ErrorMessageResourceType = typeof(UIMessages), ErrorMessageResourceName = "ComputerNameIsRequired")]
        [MaxLength(256, ErrorMessageResourceType = typeof(UIMessages), ErrorMessageResourceName = "ComputerNameIsTooLong")]
        public string SelectedRoleKey { get; set; }

        public List<AvailableRole> AvailableRoles { get; set; } = new List<AvailableRole>();

        public List<SelectListItem> SelectionItems { get; set; } = new List<SelectListItem>();

        public TimeSpan RequestedDuration { get; set; }
    }
}