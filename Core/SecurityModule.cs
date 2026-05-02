using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class SecurityModule : ISecurityModule
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SecurityModule(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public List<NavigationModuleModel> GetUserPermissions(int securityUserId)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                SqlCommand cmd = new("Security_GetUserModulePermissions", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@SecurityUserId", securityUserId);

                List<NavigationModuleModel> modules = [];

                using SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    modules.Add(new NavigationModuleModel
                    {
                        SecurityModuleId = dr.GetInt32(dr.GetOrdinal("SecurityModuleId")),
                        ModuleName       = dr.GetString(dr.GetOrdinal("ModuleName")),
                        ModuleUrl        = dr.IsDBNull(dr.GetOrdinal("ModuleUrl"))  ? null : dr.GetString(dr.GetOrdinal("ModuleUrl")),
                        ModuleIcon       = dr.IsDBNull(dr.GetOrdinal("ModuleIcon")) ? null : dr.GetString(dr.GetOrdinal("ModuleIcon")),
                        CanCreate        = dr.GetBoolean(dr.GetOrdinal("CanCreate")),
                        CanEdit          = dr.GetBoolean(dr.GetOrdinal("CanEdit")),
                        CanDelete        = dr.GetBoolean(dr.GetOrdinal("CanDelete"))
                    });
                }

                return modules;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener los módulos del usuario", ex);
            }
        }
    }
}
