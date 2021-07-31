using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api.Shared
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ApprovalState
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }
}
