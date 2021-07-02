using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Text;

namespace Lithnet.AccessManager.Server
{
    public static class DeviceExtensions
    {
        public static ClaimsIdentity ToClaimsIdentity(this IDevice device)
        {
            ClaimsIdentity identity = new ClaimsIdentity();
            identity.AddClaim(new Claim("sub", device.ObjectID));
            identity.AddClaim(new Claim("authority-type", device.AuthorityType.ToString()));
            identity.AddClaim(new Claim("authority-id", device.AuthorityId));
            identity.AddClaim(new Claim("authority-identifier", device.AuthorityDeviceId));
            identity.AddClaim(new Claim("sid", device.Sid));
            identity.AddClaim(new Claim("approval-state", device.ApprovalState.ToString()));
            identity.AddClaim(new Claim("object-type", "Computer"));

            return identity;
        }

        public static void ToCreateCommandParameters(this IDevice device, SqlCommand command)
        {
            command.Parameters.AddWithValue("@ObjectID", device.ObjectID);
            command.Parameters.AddWithValue("@ComputerName", device.ComputerName);
            command.Parameters.AddWithValue("@DnsName", device.DnsName);
            command.Parameters.AddWithValue("@ApprovalState", (int)device.ApprovalState);
            command.Parameters.AddWithValue("@AuthorityDeviceId", device.AuthorityDeviceId);
            command.Parameters.AddWithValue("@SID", device.Sid);
            command.Parameters.AddWithValue("@AgentVersion", device.AgentVersion);
            command.Parameters.AddWithValue("@OSFamily", device.OperatingSystemFamily);
            command.Parameters.AddWithValue("@OSVersion", device.OperatingSystemVersion);
        }

        public static void ToUpdateCommandParameters(this IDevice device, SqlCommand command)
        {
            command.Parameters.AddWithValue("@ObjectID", device.ObjectID);
            command.Parameters.AddWithValue("@ComputerName", device.ComputerName);
            command.Parameters.AddWithValue("@DnsName", device.DnsName);
            command.Parameters.AddWithValue("@AgentVersion", device.AgentVersion);
            command.Parameters.AddWithValue("@OSFamily", device.OperatingSystemFamily);
            command.Parameters.AddWithValue("@OSVersion", device.OperatingSystemVersion);
        }

        public static void ThrowOnInvalidStateForAuthentication(this IDevice device)
        {
            if (device.ApprovalState != ApprovalState.Approved)
            {
                throw new DeviceNotApprovedException("The device attempted to authenticate but it was not in an approved state");
            }
        }
    }
}
