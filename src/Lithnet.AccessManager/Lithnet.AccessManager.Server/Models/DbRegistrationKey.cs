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

        public DbRegistrationKey()
        {
        }

        public DbRegistrationKey(SqlDataReader reader)
        {
            this.Id = reader["Id"].CastOrDefault<long>();
            this.Key = reader["RegistrationKey"].CastOrDefault<string>();
            this.ActivationCount = reader["ActivationLimit"].CastOrDefault<int>();
            this.ActivationLimit = reader["ActivationCount"].CastOrDefault<int>();
            this.Enabled = reader["Enabled"].CastOrDefault<bool>();
            this.Name = reader["RegistrationKeyName"].CastOrDefault<string>();
        }
    }
}