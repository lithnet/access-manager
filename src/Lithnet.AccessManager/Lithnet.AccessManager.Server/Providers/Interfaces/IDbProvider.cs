using System.Data;
using System.Data.SqlClient;

namespace Lithnet.AccessManager.Server
{
    public interface IDbProvider
    {
        SqlConnection GetConnection();

        string ConnectionString { get; }

        void InitializeDb();
    }
}