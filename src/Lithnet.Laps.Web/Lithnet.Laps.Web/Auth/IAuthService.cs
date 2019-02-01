using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web.Auth
{
    public interface IAuthService
    {
        bool CanAccessPassword(string userName, string computerName);
    }
}
