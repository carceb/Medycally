using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class ExchangeRate : IExchangeRate
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public ExchangeRate(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public List<ExchangeRateModel> GetAll()
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();
                SqlCommand cmd = new("ExchangeRate_GetAll", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                using SqlDataReader dr = cmd.ExecuteReader();
                var list = new List<ExchangeRateModel>();
                while (dr.Read())
                    list.Add(Map(dr));
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener las tasas de cambio", ex);
            }
        }

        public void Save(string currencyCode, decimal rate)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();
                SqlCommand cmd = new("ExchangeRate_Save", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@CurrencyCode", currencyCode);
                cmd.Parameters.Add(new SqlParameter("@Rate", SqlDbType.Decimal) { Precision = 10, Scale = 4, Value = rate });
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al guardar la tasa de cambio", ex);
            }
        }

        private static ExchangeRateModel Map(SqlDataReader dr) => new()
        {
            CurrencyCode = dr.GetString(dr.GetOrdinal("CurrencyCode")),
            Rate         = dr.GetDecimal(dr.GetOrdinal("Rate")),
            FetchedAt    = dr.GetDateTime(dr.GetOrdinal("FetchedAt")),
        };
    }
}
