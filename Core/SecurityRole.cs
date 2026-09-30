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
            cmd.Parameters.AddWithValue("@HasGlobalScope", model.HasGlobalScope);

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
            int parentOrd = r.GetOrdinal("ParentSecurityModuleId");
            while (r.Read())
            {
                list.Add(new SecurityRoleModuleModel
                {
                    SecurityModuleId       = r.GetInt32(r.GetOrdinal("SecurityModuleId")),
                    ParentSecurityModuleId = r.IsDBNull(parentOrd) ? null : r.GetInt32(parentOrd),
                    ModuleName             = r.GetString(r.GetOrdinal("ModuleName")),
                    ModuleUrl              = r.IsDBNull(r.GetOrdinal("ModuleUrl"))  ? null : r.GetString(r.GetOrdinal("ModuleUrl")),
                    ModuleIcon             = r.IsDBNull(r.GetOrdinal("ModuleIcon")) ? null : r.GetString(r.GetOrdinal("ModuleIcon")),
                    ModuleOrder            = r.GetByte(r.GetOrdinal("ModuleOrder")),
                    CanView                = r.GetBoolean(r.GetOrdinal("CanView")),
                    CanCreate              = r.GetBoolean(r.GetOrdinal("CanCreate")),
                    CanEdit                = r.GetBoolean(r.GetOrdinal("CanEdit")),
                    CanDelete              = r.GetBoolean(r.GetOrdinal("CanDelete")),
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

        public List<SecurityModuleActionModel> GetActionsByRole(int securityRoleId)
        {
            var list = new List<SecurityModuleActionModel>();
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityModuleAction_GetByRole", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SecurityRoleId", securityRoleId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new SecurityModuleActionModel
                {
                    SecurityModuleActionId = r.GetInt32(r.GetOrdinal("SecurityModuleActionId")),
                    SecurityModuleId       = r.GetInt32(r.GetOrdinal("SecurityModuleId")),
                    ActionKey              = r.GetString(r.GetOrdinal("ActionKey")),
                    ActionName             = r.GetString(r.GetOrdinal("ActionName")),
                    ActionOrder            = r.GetByte(r.GetOrdinal("ActionOrder")),
                    IsAllowed              = r.GetBoolean(r.GetOrdinal("IsAllowed")),
                });
            }
            return list;
        }

        public void SaveActions(int securityRoleId, List<int> allowedActionIds)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityRoleModuleAction_SaveBatch", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SecurityRoleId",   securityRoleId);
            cmd.Parameters.AddWithValue("@AllowedActionIds", string.Join(',', allowedActionIds ?? new List<int>()));
            cmd.ExecuteNonQuery();
        }

        public List<string> GetUserActions(int securityUserId, string moduleUrl)
        {
            var list = new List<string>();
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityModuleAction_GetByUserAndModule", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SecurityUserId", securityUserId);
            cmd.Parameters.AddWithValue("@ModuleUrl",      moduleUrl);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(r.GetString(r.GetOrdinal("ActionKey")));
            return list;
        }
    }
}
