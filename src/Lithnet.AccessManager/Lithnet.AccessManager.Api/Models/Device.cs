using System;
using System.Data.SqlClient;
using System.Security.Claims;

namespace Lithnet.AccessManager.Api
{
    public class Device
    {
        public long Id { get; set; }

        public string ObjectId { get; set; }

        public string AgentVersion { get; set; }

        public string ComputerName { get; set; }

        public string DnsName { get; set; }

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }

        public AuthorityType AuthorityType { get; set; }

        public string Authority { get; set; }

        public string AuthorityDeviceId { get; set; }

        public ApprovalState ApprovalState { get; set; }

        public string OperatingSystemFamily { get; set; }

        public string OperatingSystemVersion { get; set; }

        public Device()
        {
        }

        public Device(SqlDataReader reader)
        {
            this.Id = reader["Id"].CastOrDefault<long>();
            this.ObjectId = reader["ObjectId"].CastOrDefault<string>();
            this.AgentVersion = reader["AgentVersion"].CastOrDefault<string>();
            this.ComputerName = reader["ComputerName"].CastOrDefault<string>();
            this.DnsName = reader["DnsName"].CastOrDefault<string>();
            this.Created = reader["Created"].CastOrDefault<DateTime>();
            this.Modified = reader["Modified"].CastOrDefault<DateTime>();
            this.AuthorityType = (AuthorityType)reader["AuthorityType"].CastOrDefault<int>();
            this.Authority = reader["Authority"].CastOrDefault<string>();
            this.AuthorityDeviceId = reader["AuthorityDeviceId"].CastOrDefault<string>();
            this.ApprovalState = (ApprovalState)reader["ApprovalState"].CastOrDefault<int>();
            this.OperatingSystemFamily = reader["OperatingSystemFamily"].CastOrDefault<string>();
            this.OperatingSystemVersion = reader["OperatingSystemVersion"].CastOrDefault<string>();
        }

        public ClaimsIdentity ToClaimsIdentity()
        {
            ClaimsIdentity identity = new ClaimsIdentity();
            identity.AddClaim(new Claim("sub", this.ObjectId));
            identity.AddClaim(new Claim("authority-type", this.AuthorityType.ToString()));
            identity.AddClaim(new Claim("authority", this.Authority));
            identity.AddClaim(new Claim("authority-identifier", this.AuthorityDeviceId));
            identity.AddClaim(new Claim("object-type", "Computer"));

            return identity;
        }

        public void ToCommandParameters(SqlCommand command)
        {
            command.Parameters.AddWithValue("@ObjectID", this.ObjectId);
            command.Parameters.AddWithValue("@ComputerName", this.ComputerName);
            command.Parameters.AddWithValue("@DnsName", this.DnsName);
            command.Parameters.AddWithValue("@ApprovalState", (int)this.ApprovalState);
            command.Parameters.AddWithValue("@AuthorityType", (int)this.AuthorityType);
            command.Parameters.AddWithValue("@Authority", this.Authority);
            command.Parameters.AddWithValue("@AuthorityDeviceId", this.AuthorityDeviceId);
            command.Parameters.AddWithValue("@AgentVersion", this.AgentVersion);
            command.Parameters.AddWithValue("@OSFamily", this.OperatingSystemFamily);
            command.Parameters.AddWithValue("@OSVersion", this.OperatingSystemVersion);
        }
    }
}