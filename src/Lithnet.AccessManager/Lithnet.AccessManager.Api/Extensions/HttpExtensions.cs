using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Lithnet.AccessManager.Api
{
    public static class HttpExtensions
    {
        public static string GetClaimOrThrow(this HttpContext context, string claimName)
        {
            var c = context.User.FindFirstValue(claimName);

            if (string.IsNullOrWhiteSpace(c))
            {
                throw new BadRequestException($"The claim '{c}' was missing from the claim");
            }

            return c;
        }

        public static string GetDeviceIdOrThrow(this HttpContext context)
        {
            return context.GetClaimOrThrow(ClaimTypes.NameIdentifier);
        }
    }
}
