using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.Laps.Auth
{
    public interface IAuthService
    {
        bool CanAccessPassword(string userName, string computerName);
    }
}
