using System.Data.SqlClient;
using System.Security.Principal;

namespace Lithnet.AccessManager.Server.Providers
{
    public class DbAmsGroup : IAmsGroup
    {
        private string sid;

        public DbAmsGroup()
        {
        }

        public DbAmsGroup(SqlDataReader reader)
        {
            this.Id = reader["Id"].CastOrDefault<long>();
            this.Name = reader["Name"].CastOrDefault<string>();
            this.Description= reader["Description"].CastOrDefault<string>();
            this.Sid = reader["Sid"].CastOrDefault<string>();
        }

        public long Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Sid
        {
            get => this.sid;
            set
            {
                this.sid = value;
                if (string.IsNullOrWhiteSpace(value))
                {
                    this.SecurityIdentifier = null;
                }
                else
                {
                    this.SecurityIdentifier = new SecurityIdentifier(value);
                }
            }
        }

        public SecurityIdentifier SecurityIdentifier { get; set; }
    }
}