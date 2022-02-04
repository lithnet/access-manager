using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Service
{
    public static class DateTimeExtensions
    {
        public static long? ToUnixEpochMilliseconds(this DateTime? dateTime)
        {
            if (dateTime.HasValue)
            {
                return (long)((TimeSpan)(dateTime.Value.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalMilliseconds;
            }

            return null;
        }
    }
}
