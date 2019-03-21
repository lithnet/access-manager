using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web
{
    public interface IRateLimiter
    {
        bool IsRateLimitExceeded(LapRequestModel model, UserPrincipal p, HttpRequestBase r);
    }
}
