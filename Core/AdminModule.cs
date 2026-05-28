using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class AdminModule : IAdminModule
    {
        private readonly ISqlConnectionFactory _db;

        public AdminModule(ISqlConnectionFactory db) => _db = db;

        public List<SecurityModuleAdminModel> GetAll()
        {
            var list = new List<SecurityModuleAdminModel>();
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityModule_GetAll", conn) { CommandType = CommandType.StoredProcedure };
            using var r   = cmd.ExecuteReader();

            int parentIdOrd   = r.GetOrdinal("ParentSecurityModuleId");
            int parentNameOrd = r.GetOrdinal("ParentModuleName");
            int urlOrd        = r.GetOrdinal("ModuleUrl");
            int iconOrd       = r.GetOrdinal("ModuleIcon");

            while (r.Read())
            {
                list.Add(new SecurityModuleAdminModel
                {
                    SecurityModuleId       = r.GetInt32(r.GetOrdinal("SecurityModuleId")),
                    ParentSecurityModuleId = r.IsDBNull(parentIdOrd)   ? null : r.GetInt32(parentIdOrd),
                    ParentModuleName       = r.IsDBNull(parentNameOrd) ? null : r.GetString(parentNameOrd),
                    ModuleName             = r.GetString(r.GetOrdinal("ModuleName")),
                    ModuleUrl              = r.IsDBNull(urlOrd)  ? null : r.GetString(urlOrd),
                    ModuleIcon             = r.IsDBNull(iconOrd) ? null : r.GetString(iconOrd),
                    ModuleOrder            = r.GetByte(r.GetOrdinal("ModuleOrder")),
                    IsActive               = r.GetBoolean(r.GetOrdinal("IsActive")),
                });
            }
            return list;
        }

        public int AddOrEdit(SecurityModuleAdminModel model)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityModule_AddOrEdit", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SecurityModuleId",       model.SecurityModuleId);
            cmd.Parameters.AddWithValue("@ParentSecurityModuleId", (object?)model.ParentSecurityModuleId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ModuleName",             model.ModuleName);
            cmd.Parameters.AddWithValue("@ModuleUrl",              (object?)model.ModuleUrl  ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ModuleIcon",             (object?)model.ModuleIcon ?? DBNull.Value);
            cmd.Parameters.Add("@ModuleOrder", SqlDbType.TinyInt).Value = (byte)model.ModuleOrder;
            cmd.Parameters.AddWithValue("@IsActive",               model.IsActive);

            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : model.SecurityModuleId;
        }

        public void Delete(int securityModuleId)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityModule_Delete", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SecurityModuleId", securityModuleId);
            cmd.ExecuteNonQuery();
        }
    }
}
