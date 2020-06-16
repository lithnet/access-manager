using System;
using Lithnet.AccessManager.Web.Authorization;
using Lithnet.AccessManager.Web.Models;

namespace Lithnet.AccessManager.Web.Internal
{
    public class AuditableAction
    {
        public bool IsSuccess { get; set; }

        public int EventID { get; set; }

        public LapRequestModel RequestModel { get; set; }

        public AuthorizationResponse AuthzResponse { get; set; }

        public IUser User { get; set; }

        public IComputer Computer { get; set; }

        public Exception Exception { get; set; }

        public string Message { get; set; }

        public string ComputerExpiryDate { get; set; }
    }
}
