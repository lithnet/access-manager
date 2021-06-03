using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api
{
    public class DbPasswordData
    {
        public long Id { get; set; }

        public string PasswordData { get; set; }

        public DateTime EffectiveDate { get; set; }

        public DateTime RetiredDate { get; set; }

        public string RequestId { get; set; }

        public string AccountName { get; set; }

        public DbPasswordData()
        {
        }

        public DbPasswordData(SqlDataReader reader)
        {
            this.Id = reader["Id"].CastOrDefault<long>();
            this.PasswordData = reader["PasswordData"].CastOrDefault<string>();
            this.EffectiveDate= reader["EffectiveDate"].CastOrDefault<DateTime>();
            this.RetiredDate = reader["RetiredDate"].CastOrDefault<DateTime>();
            this.RequestId = reader["RequestId"].CastOrDefault<string>();
            this.AccountName = reader["AccountName"].CastOrDefault<string>();
        }
    }
}
