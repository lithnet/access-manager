﻿using System;

namespace Lithnet.AccessManager.Server
{
    public class BitLockerRecoveryPassword
    {
        public string RecoveryPassword { get; set; }

        public DateTime? Created { get; set; }

        public string VolumeID { get; set; }

        public string PasswordID { get; set; }
    }
}
