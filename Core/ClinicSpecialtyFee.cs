using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class ClinicSpecialtyFee : IClinicSpecialtyFee
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public ClinicSpecialtyFee(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public List<ClinicSpecialtyFeeModel> GetByClinic(int clinicId)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();
                SqlCommand cmd = new("ClinicSpecialtyFee_GetByClinic", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClinicId", clinicId);

                using SqlDataReader dr = cmd.ExecuteReader();
                var list = new List<ClinicSpecialtyFeeModel>();
                while (dr.Read())
                    list.Add(Map(dr));
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener las tarifas de la clínica", ex);
            }
        }

        public int Save(ClinicSpecialtyFeeModel m, int updatedByUserId)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();
                SqlCommand cmd = new("ClinicSpecialtyFee_Save", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClinicId",     m.ClinicId);
                cmd.Parameters.AddWithValue("@SpecialtyId",  m.SpecialtyId);
                cmd.Parameters.Add(new SqlParameter("@FeeUSD",           SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = m.FeeUSD });
                cmd.Parameters.Add(new SqlParameter("@FeeVES",           SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = m.FeeVES });
                cmd.Parameters.Add(new SqlParameter("@ExchangeRateUsed", SqlDbType.Decimal) { Precision = 10, Scale = 4, Value = m.ExchangeRateUsed });
                cmd.Parameters.AddWithValue("@UpdatedByUserId", updatedByUserId);

                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al guardar la tarifa", ex);
            }
        }

        private static ClinicSpecialtyFeeModel Map(SqlDataReader dr) => new()
        {
            ClinicSpecialtyFeeId = dr.GetInt32(dr.GetOrdinal("ClinicSpecialtyFeeId")),
            SpecialtyId          = dr.GetInt32(dr.GetOrdinal("SpecialtyId")),
            SpecialtyName        = dr.GetString(dr.GetOrdinal("SpecialtyName")),
            FeeUSD               = dr.GetDecimal(dr.GetOrdinal("FeeUSD")),
            FeeVES               = dr.GetDecimal(dr.GetOrdinal("FeeVES")),
            ExchangeRateUsed     = dr.GetDecimal(dr.GetOrdinal("ExchangeRateUsed")),
            UpdatedAt            = dr.IsDBNull(dr.GetOrdinal("UpdatedAt")) ? null : dr.GetDateTime(dr.GetOrdinal("UpdatedAt")),
        };
    }
}
