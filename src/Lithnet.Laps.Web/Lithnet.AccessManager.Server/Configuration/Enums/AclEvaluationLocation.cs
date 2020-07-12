using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lithnet.AccessManager.Server
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AclEvaluationLocation
    {
        ComputerDomain = 0,

        UserDomain = 1,

        WebAppDomain = 2,
    }
}