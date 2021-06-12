﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    public enum AuthorityType
    {
        None = 0,
        ActiveDirectory = 1,
        AzureActiveDirectory = 2,
        SelfAsserted = 3
    }
}