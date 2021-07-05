using System.Data.SqlClient;

namespace Lithnet.AccessManager.Server
{
    public class DbRegistrationKey : IRegistrationKey
    {
        public long Id { get; set; }

        public string Key { get; set; }

        public int ActivationCount { get; set; }

        public int ActivationLimit { get; set; }

        public bool Enabled { get; set; }

        public string Name { get; set; }

        public bool ApprovalRequired { get; set; }

        public DbRegistrationKey()
        {
        }

        public DbRegistrationKey(SqlDataReader reader)
        {
            this.Id = reader["Id"].CastOrDefault<long>();
            this.Key = reader["RegistrationKey"].CastOrDefault<string>();
            this.ActivationCount = reader["ActivationCount"].CastOrDefault<int>();
            this.ActivationLimit = reader["ActivationLimit"].CastOrDefault<int>();
            this.ApprovalRequired = reader["ApprovalRequired"].CastOrDefault<bool>();
            this.Enabled = reader["Enabled"].CastOrDefault<bool>();
            this.Name = reader["RegistrationKeyName"].CastOrDefault<string>();
        }
    }
}