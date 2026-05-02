using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
	public class ClinicType : IClinicType
	{
		private readonly ISqlConnectionFactory _connectionFactory;
		public ClinicType(ISqlConnectionFactory connectionFactory)
			=> _connectionFactory = connectionFactory;

		public List<ClinicTypeModel> GetAll()
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();
				SqlCommand cmd = new("ClinicType_GetAll", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				var list = new List<ClinicTypeModel>();
				using SqlDataReader dr = cmd.ExecuteReader();
				while (dr.Read())
					list.Add(new ClinicTypeModel
					{
						ClinicTypeId   = dr.GetInt32(dr.GetOrdinal("ClinicTypeId")),
						ClinicTypeName = dr.IsDBNull(dr.GetOrdinal("ClinicTypeName")) ? null : dr.GetString(dr.GetOrdinal("ClinicTypeName"))
					});
				return list;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener los tipos de clínica", ex);
			}
		}
	}
}
