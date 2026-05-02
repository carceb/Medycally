using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class SecurityUser : ISecurityUser
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SecurityUser(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public SecurityUserModel? Login(string email, string passwordHash)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                SqlCommand cmd = new("Security_UserLogin", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@UserEmail", email);
                cmd.Parameters.AddWithValue("@UserPasswordHash", passwordHash);

                using SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    int doctorIdOrd = dr.GetOrdinal("DoctorId");
                    return new SecurityUserModel
                    {
                        SecurityUserId = dr.GetInt32(dr.GetOrdinal("SecurityUserId")),
                        UserName       = dr.GetString(dr.GetOrdinal("UserName")),
                        UserEmail      = dr.GetString(dr.GetOrdinal("UserEmail")),
                        UserIdNumber   = dr.GetInt32(dr.GetOrdinal("UserIdNumber")),
                        SecurityRoleId = dr.GetInt32(dr.GetOrdinal("SecurityRoleId")),
                        RoleName       = dr.GetString(dr.GetOrdinal("RoleName")),
                        RoleLevel      = dr.GetByte(dr.GetOrdinal("RoleLevel")),
                        DoctorId       = dr.IsDBNull(doctorIdOrd) ? null : dr.GetInt32(doctorIdOrd)
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al autenticar el usuario", ex);
            }
        }
    }
}
