using Microsoft.Data.SqlClient;

namespace Medycally.Core.Data
{
    public interface ISqlConnectionFactory
    {
        SqlConnection CreateConnection();
    }
}
