﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Web
{
    public class PowerShellAuthorizationResponse
    {
        public bool IsDenied { get; set; }

        public bool IsAllowed { get; set; }
    }
}
