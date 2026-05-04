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

                using SqlCommand cmd = new("Security_UserLogin", connection)
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

        public SecurityUserModel? GetByToken(string token)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                using SqlCommand cmd = new("SecurityUser_GetByToken", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Token", token);

                using SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    return new SecurityUserModel
                    {
                        SecurityUserId = dr.GetInt32(dr.GetOrdinal("SecurityUserId")),
                        UserName       = dr.GetString(dr.GetOrdinal("UserName")),
                        UserEmail      = dr.GetString(dr.GetOrdinal("UserEmail"))
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el token de activación", ex);
            }
        }

        public bool Activate(string token, string passwordHash)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                using SqlCommand cmd = new("SecurityUser_Activate", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Token", token);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                var result = cmd.ExecuteScalar();
                return result != null && Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al activar la cuenta", ex);
            }
        }

        public SecurityUserModel? ForgotPassword(string email)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                using SqlCommand cmd = new("SecurityUser_ForgotPassword", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@UserEmail", email);

                using SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    int resetTokenOrd = dr.GetOrdinal("ResetToken");
                    return new SecurityUserModel
                    {
                        SecurityUserId = dr.GetInt32(dr.GetOrdinal("SecurityUserId")),
                        UserName       = dr.GetString(dr.GetOrdinal("UserName")),
                        UserEmail      = dr.GetString(dr.GetOrdinal("UserEmail")),
                        ResetToken     = dr.IsDBNull(resetTokenOrd) ? null : dr.GetString(resetTokenOrd)
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al procesar la solicitud de restablecimiento", ex);
            }
        }

        public SecurityUserModel? GetByResetToken(string token)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                using SqlCommand cmd = new("SecurityUser_GetByResetToken", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Token", token);

                using SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    return new SecurityUserModel
                    {
                        SecurityUserId = dr.GetInt32(dr.GetOrdinal("SecurityUserId")),
                        UserName       = dr.GetString(dr.GetOrdinal("UserName")),
                        UserEmail      = dr.GetString(dr.GetOrdinal("UserEmail"))
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al validar el token de restablecimiento", ex);
            }
        }

        public bool ResetPassword(string token, string passwordHash)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                using SqlCommand cmd = new("SecurityUser_ResetPassword", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Token", token);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);

                var result = cmd.ExecuteScalar();
                return result != null && Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al restablecer la contraseña", ex);
            }
        }
    }
}
