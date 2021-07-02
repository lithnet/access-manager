using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    public class Constants
    {
        public const string AuthZPolicyComputers = "ComputerOnly";
        public const string AuthZPolicyAuthorityAzureAd = "Authority-AzureAd";
        public const string AuthZPolicyAuthorityAms = "Authority-Ams";
        public const string AuthZPolicyAuthorityAd = "Authority-Ad";
        public const string AuthZPolicyApprovedClient = "ApprovedClients";
    }
}
