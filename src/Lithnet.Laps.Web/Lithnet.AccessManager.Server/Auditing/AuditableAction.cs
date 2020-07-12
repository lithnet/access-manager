using System;
using Lithnet.AccessManager.Server.Authorization;

namespace Lithnet.AccessManager.Server.Auditing
{
    public class AuditableAction
    {
        public bool IsSuccess { get; set; }

        public int EventID { get; set; }

        public string RequestedComputerName { get; set; }

        public string RequestReason { get; set; }

        public AuthorizationResponse AuthzResponse { get; set; }

        public IUser User { get; set; }

        public IComputer Computer { get; set; }

        public Exception Exception { get; set; }

        public string Message { get; set; }

        public string ComputerExpiryDate { get; set; }
    }
}
