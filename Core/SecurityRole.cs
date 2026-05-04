using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class SecurityRole : ISecurityRole
    {
        private readonly ISqlConnectionFactory _db;

        public SecurityRole(ISqlConnectionFactory db) => _db = db;

        public int AddOrEdit(SecurityRoleModel model)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityRole_AddOrEdit", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SecurityRoleId", model.SecurityRoleId);
            cmd.Parameters.AddWithValue("@RoleName",       model.RoleName);
            cmd.Parameters.Add("@RoleLevel", SqlDbType.TinyInt).Value = (byte)model.RoleLevel;

            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : model.SecurityRoleId;
        }

        public void Delete(int securityRoleId)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityRole_Delete", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SecurityRoleId", securityRoleId);
            cmd.ExecuteNonQuery();
        }

        public List<SecurityRoleModuleModel> GetModules(int securityRoleId)
        {
            var list = new List<SecurityRoleModuleModel>();
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityRoleModule_GetByRole", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SecurityRoleId", securityRoleId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new SecurityRoleModuleModel
                {
                    SecurityModuleId = r.GetInt32(r.GetOrdinal("SecurityModuleId")),
                    ModuleName       = r.GetString(r.GetOrdinal("ModuleName")),
                    ModuleUrl        = r.IsDBNull(r.GetOrdinal("ModuleUrl"))  ? null : r.GetString(r.GetOrdinal("ModuleUrl")),
                    ModuleIcon       = r.IsDBNull(r.GetOrdinal("ModuleIcon")) ? null : r.GetString(r.GetOrdinal("ModuleIcon")),
                    ModuleOrder      = r.GetByte(r.GetOrdinal("ModuleOrder")),
                    CanView          = r.GetBoolean(r.GetOrdinal("CanView")),
                    CanCreate        = r.GetBoolean(r.GetOrdinal("CanCreate")),
                    CanEdit          = r.GetBoolean(r.GetOrdinal("CanEdit")),
                    CanDelete        = r.GetBoolean(r.GetOrdinal("CanDelete")),
                });
            }
            return list;
        }

        public void SaveModule(int securityRoleId, SecurityRoleModuleModel module)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityRoleModule_Save", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SecurityRoleId",   securityRoleId);
            cmd.Parameters.AddWithValue("@SecurityModuleId", module.SecurityModuleId);
            cmd.Parameters.AddWithValue("@CanView",          module.CanView);
            cmd.Parameters.AddWithValue("@CanCreate",        module.CanCreate);
            cmd.Parameters.AddWithValue("@CanEdit",          module.CanEdit);
            cmd.Parameters.AddWithValue("@CanDelete",        module.CanDelete);
            cmd.ExecuteNonQuery();
        }
    }
}
