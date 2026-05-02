using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
	public class Reason : IReason
	{
		private readonly ISqlConnectionFactory _connectionFactory;

		public Reason(ISqlConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public List<ReasonModel> GetAll(int specialtyId)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();

				SqlCommand cmd = new(
					"SELECT ReasonId, ReasonName, SpecialtyId " +
					"FROM dbo.Reason " +
					"WHERE SpecialtyId = @SpecialtyId " +
					"ORDER BY ReasonName",
					connection)
				{
					CommandType = CommandType.Text
				};
				cmd.Parameters.AddWithValue("@SpecialtyId", specialtyId);

				List<ReasonModel> reasons = [];

				using SqlDataReader dr = cmd.ExecuteReader();
				while (dr.Read())
				{
					reasons.Add(new ReasonModel
					{
						ReasonId    = dr.GetInt32(dr.GetOrdinal("ReasonId")),
						ReasonName  = dr.GetString(dr.GetOrdinal("ReasonName")),
						SpecialtyId = dr.GetInt32(dr.GetOrdinal("SpecialtyId"))
					});
				}

				return reasons;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener los motivos de consulta", ex);
			}
		}
	}
}
