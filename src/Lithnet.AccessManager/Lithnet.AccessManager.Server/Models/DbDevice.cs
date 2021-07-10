using System;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Security.Principal;

namespace Lithnet.AccessManager.Server
{
    public class DbDevice : IComputer, IDevice
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
                    return $"AccessManager\\{this.ComputerName}";
                }
                else
                {
                    return $"{this.AuthorityId}\\{this.ComputerName}";
                }
            }
        }

        public string AuthorityId { get; set; }

        public string AuthorityDeviceId { get; set; }

        public SecurityIdentifier SecurityIdentifier { get; set; }

        public ApprovalState ApprovalState { get; set; }

        public string Sid => this.SecurityIdentifier.ToString();

        public string OperatingSystemFamily { get; set; }

        public string OperatingSystemVersion { get; set; }

        public bool Disabled { get; set; }

        public DbDevice()
        {
        }

        public DbDevice(SqlDataReader reader)
        {
            this.Id = reader["Id"].CastOrDefault<long>();
            this.Disabled = reader["Disabled"].CastOrDefault<bool>();
            this.ObjectID = reader["ObjectId"].CastOrDefault<string>();
            this.AgentVersion = reader["AgentVersion"].CastOrDefault<string>();
            this.ComputerName = reader["ComputerName"].CastOrDefault<string>();
            this.DnsName = reader["DnsName"].CastOrDefault<string>();
            this.Created = reader["Created"].CastOrDefault<DateTime>();
            this.Modified = reader["Modified"].CastOrDefault<DateTime>();
            this.AuthorityType = (AuthorityType)reader["AuthorityType"].CastOrDefault<int>();
            this.AuthorityId = reader["AuthorityId"].CastOrDefault<string>();
            this.AuthorityDeviceId = reader["AuthorityDeviceId"].CastOrDefault<string>();
            this.SecurityIdentifier = new SecurityIdentifier(reader["SID"].CastOrDefault<string>());
            this.ApprovalState = (ApprovalState)reader["ApprovalState"].CastOrDefault<int>();
            this.OperatingSystemFamily = reader["OperatingSystemFamily"].CastOrDefault<string>();
            this.OperatingSystemVersion = reader["OperatingSystemVersion"].CastOrDefault<string>();
        }
    }
}