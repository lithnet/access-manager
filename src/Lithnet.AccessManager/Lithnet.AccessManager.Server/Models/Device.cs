using System;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Security.Principal;

namespace Lithnet.AccessManager.Server
{
    public class Device : IComputer
    {
        public long Id { get; set; }

        public string ObjectID { get; set; }

        public string AgentVersion { get; set; }

        public string ComputerName { get; set; }

        public string DnsName { get; set; }

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }

        public AuthorityType AuthorityType { get; set; }

        public string Description => null;

        public string DisplayName => this.ComputerName;

        public string DnsHostName => this.DnsName;

        public string Name => this.ComputerName;

        public string FullyQualifiedName
        {
            get
            {
                if (this.AuthorityType == AuthorityType.AzureActiveDirectory)
                {
                    return $"AzureAD\\{this.ComputerName}";
                }
                else if (this.AuthorityType == AuthorityType.Ams)
                {
                    return $"AMS\\{this.ComputerName}";
                }
                else
                {
                    return $"{this.Authority}\\{this.ComputerName}";
                }
            }
        }

        public string Authority { get; set; }

        public string AuthorityDeviceId { get; set; }

        public SecurityIdentifier SecurityIdentifier { get; set; }

        public ApprovalState ApprovalState { get; set; }

        public string Sid => this.SecurityIdentifier.ToString();

        public string OperatingSystemFamily { get; set; }

        public string OperatingSystemVersion { get; set; }

        public Device()
        {
        }

        public Device(SqlDataReader reader)
        {
            this.Id = reader["Id"].CastOrDefault<long>();
            this.ObjectID = reader["ObjectId"].CastOrDefault<string>();
            this.AgentVersion = reader["AgentVersion"].CastOrDefault<string>();
            this.ComputerName = reader["ComputerName"].CastOrDefault<string>();
            this.DnsName = reader["DnsName"].CastOrDefault<string>();
            this.Created = reader["Created"].CastOrDefault<DateTime>();
            this.Modified = reader["Modified"].CastOrDefault<DateTime>();
            this.AuthorityType = (AuthorityType)reader["AuthorityType"].CastOrDefault<int>();
            this.Authority = reader["Authority"].CastOrDefault<string>();
            this.AuthorityDeviceId = reader["AuthorityDeviceId"].CastOrDefault<string>();
            this.SecurityIdentifier = new SecurityIdentifier(reader["SID"].CastOrDefault<string>());
            this.ApprovalState = (ApprovalState)reader["ApprovalState"].CastOrDefault<int>();
            this.OperatingSystemFamily = reader["OperatingSystemFamily"].CastOrDefault<string>();
            this.OperatingSystemVersion = reader["OperatingSystemVersion"].CastOrDefault<string>();
        }

        public ClaimsIdentity ToClaimsIdentity()
        {
            ClaimsIdentity identity = new ClaimsIdentity();
            identity.AddClaim(new Claim("sub", this.ObjectID));
            identity.AddClaim(new Claim("authority-type", this.AuthorityType.ToString()));
            identity.AddClaim(new Claim("authority", this.Authority));
            identity.AddClaim(new Claim("authority-identifier", this.AuthorityDeviceId));
            identity.AddClaim(new Claim("sid", this.Sid));
            identity.AddClaim(new Claim("object-type", "Computer"));

            return identity;
        }

        public void ToCreateCommandParameters(SqlCommand command)
        {
            command.Parameters.AddWithValue("@ObjectID", this.ObjectID);
            command.Parameters.AddWithValue("@ComputerName", this.ComputerName);
            command.Parameters.AddWithValue("@DnsName", this.DnsName);
            command.Parameters.AddWithValue("@ApprovalState", (int)this.ApprovalState);
            command.Parameters.AddWithValue("@AuthorityType", (int)this.AuthorityType);
            command.Parameters.AddWithValue("@Authority", this.Authority);
            command.Parameters.AddWithValue("@AuthorityDeviceId", this.AuthorityDeviceId);
            command.Parameters.AddWithValue("@SID", this.Sid);
            command.Parameters.AddWithValue("@AgentVersion", this.AgentVersion);
            command.Parameters.AddWithValue("@OSFamily", this.OperatingSystemFamily);
            command.Parameters.AddWithValue("@OSVersion", this.OperatingSystemVersion);
        }

        public void ToUpdateCommandParameters(SqlCommand command)
        {
            command.Parameters.AddWithValue("@ObjectID", this.ObjectID);
            command.Parameters.AddWithValue("@ComputerName", this.ComputerName);
            command.Parameters.AddWithValue("@DnsName", this.DnsName);
            command.Parameters.AddWithValue("@AgentVersion", this.AgentVersion);
            command.Parameters.AddWithValue("@OSFamily", this.OperatingSystemFamily);
            command.Parameters.AddWithValue("@OSVersion", this.OperatingSystemVersion);
        }
    }
}