using System.Data.SqlClient;

namespace Lithnet.AccessManager.Server
{
    public interface ISqlInstanceProvider
    {
        string ConnectionString { get; }

        SqlConnection GetConnection();
        void InitializeDb();
    }
}