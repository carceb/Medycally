using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace Medycally.Core
{
    public class AdminUser : IAdminUser
    {
        private readonly ISqlConnectionFactory _db;

        public AdminUser(ISqlConnectionFactory db) => _db = db;

        public List<AdminUserModel> GetAll()
        {
            var list = new List<AdminUserModel>();
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityUser_GetAll", conn) { CommandType = CommandType.StoredProcedure };
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                int doctorIdOrd   = r.GetOrdinal("DoctorId");
                int doctorNameOrd = r.GetOrdinal("DoctorName");
                list.Add(new AdminUserModel
                {
                    SecurityUserId = Convert.ToInt32(r["SecurityUserId"]),
                    UserName       = r["UserName"]     == DBNull.Value ? null : r["UserName"].ToString(),
                    UserEmail      = r["UserEmail"]    == DBNull.Value ? null : r["UserEmail"].ToString(),
                    UserIdNumber   = r["UserIdNumber"] == DBNull.Value ? 0 : Convert.ToInt32(r["UserIdNumber"]),
                    SecurityRoleId = Convert.ToInt32(r["SecurityRoleId"]),
                    RoleName       = r["RoleName"]     == DBNull.Value ? null : r["RoleName"].ToString(),
                    RoleLevel      = r["RoleLevel"]    == DBNull.Value ? 0 : Convert.ToInt32(r["RoleLevel"]),
                    StatusId       = Convert.ToInt32(r["StatusId"]),
                    IsActivated    = r["IsActivated"]  != DBNull.Value && Convert.ToBoolean(r["IsActivated"]),
                    DoctorId       = r.IsDBNull(doctorIdOrd)   ? null : r.GetInt32(doctorIdOrd),
                    DoctorName     = r.IsDBNull(doctorNameOrd) ? null : r.GetString(doctorNameOrd),
                });
            }
            return list;
        }

        public AdminUserModel AddOrEdit(AdminUserModel model)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityUser_AddOrEdit", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SecurityUserId", model.SecurityUserId);
            cmd.Parameters.AddWithValue("@UserName",       (object?)model.UserName  ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserEmail",      (object?)model.UserEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserIdNumber",   model.UserIdNumber);
            cmd.Parameters.AddWithValue("@SecurityRoleId", model.SecurityRoleId);
            cmd.Parameters.AddWithValue("@StatusId",       model.StatusId);
            cmd.Parameters.AddWithValue("@DoctorId",       model.DoctorId.HasValue ? (object)model.DoctorId.Value : DBNull.Value);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                model.SecurityUserId  = Convert.ToInt32(reader["SecurityUserId"]);
                int tokenOrd          = reader.GetOrdinal("ActivationToken");
                model.ActivationToken = reader.IsDBNull(tokenOrd) ? null : reader.GetString(tokenOrd);
            }
            return model;
        }

        public void Delete(int securityUserId)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityUser_Delete", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SecurityUserId", securityUserId);
            cmd.ExecuteNonQuery();
        }

        public List<SecurityRoleModel> GetAllRoles()
        {
            var list = new List<SecurityRoleModel>();
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityRole_GetAll", conn) { CommandType = CommandType.StoredProcedure };
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new SecurityRoleModel
                {
                    SecurityRoleId = Convert.ToInt32(r["SecurityRoleId"]),
                    RoleName       = r["RoleName"].ToString() ?? string.Empty,
                    RoleLevel      = r["RoleLevel"] == DBNull.Value ? 0 : Convert.ToInt32(r["RoleLevel"]),
                });
            }
            return list;
        }

        public string? ResendToken(int securityUserId)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("SecurityUser_ResendToken", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("@SecurityUserId", securityUserId);
            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? null : result.ToString();
        }

        private static string HashSha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return string.Concat(bytes.Select(b => b.ToString("x2")));
        }
    }
}
