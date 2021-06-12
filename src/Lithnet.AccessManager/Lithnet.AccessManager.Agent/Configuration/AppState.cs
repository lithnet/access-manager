﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Lithnet.AccessManager.Agent.Configuration
{
    public class AppState
    {
        public bool IsRegistered { get; set; }

        public string RegistrationKey { get; set; }

        public string ClientId { get; set; }

        public string CheckRegistrationUrl { get; set; }

        public RegistrationState RegistrationState { get; set; }
    }
}