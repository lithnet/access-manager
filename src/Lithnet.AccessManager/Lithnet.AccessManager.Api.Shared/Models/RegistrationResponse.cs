using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Api.Shared
{
    public class RegistrationResponse
    {
        public ApprovalState ApprovalState { get; set; }

        public string ClientId { get; set; }
    }
}