using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
	public class Geography : IGeography
	{
		private readonly ISqlConnectionFactory _connectionFactory;
		public Geography(ISqlConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public List<GeographyModel> GetAllStates()
		{
			List<GeographyModel> states = new();

			try
			{
				using (SqlConnection sqlConnection = _connectionFactory.CreateConnection())
				{
					using (SqlCommand cmd = new SqlCommand("SELECT * FROM State ORDER BY StateName", sqlConnection))
					{
						if (sqlConnection.State == ConnectionState.Closed)
						{
							sqlConnection.Open();
						}

						using (SqlDataReader dr = cmd.ExecuteReader())
						{
							while (dr.Read())
							{
								states.Add(new GeographyModel
								{
									StateId = dr.GetInt32(dr.GetOrdinal("StateId")),
									StateName = dr.GetString(dr.GetOrdinal("StateName"))
								});
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener los estados", ex);
			}

			return states;
		}

		public List<GeographyModel> GetStateById(int stateId)
		{
			List<GeographyModel> state = new();

			try
			{
				using (SqlConnection sqlConnection = _connectionFactory.CreateConnection())
				{
					if (sqlConnection.State == ConnectionState.Closed)
					{
						sqlConnection.Open();
					}

					using (SqlCommand cmd = new SqlCommand("SELECT * FROM State WHERE StateId = @StateId", sqlConnection))
					{
						cmd.Parameters.AddWithValue("@StateId", stateId);
						using (SqlDataReader dr = cmd.ExecuteReader())
						{
							while (dr.Read())
							{
								state.Add(new GeographyModel
								{
									StateId = (int)dr["StateId"],
									StateName = (string)dr["StateName"]
								});
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw;
			}

			return state;
		}

		public List<GeographyModel> GetAllMunicipalities()
		{
			List<GeographyModel> municipalities = new();

			try
			{
				using (SqlConnection sqlConnection = _connectionFactory.CreateConnection())
				{
					if (sqlConnection.State == ConnectionState.Closed)
						sqlConnection.Open();

					using (SqlCommand cmd = new SqlCommand(
						"SELECT MunicipalityId, MunicipalityName, StateId FROM dbo.Municipality ORDER BY MunicipalityName",
						sqlConnection))
					using (SqlDataReader dr = cmd.ExecuteReader())
					{
						while (dr.Read())
						{
							municipalities.Add(new GeographyModel
							{
								MunicipalityId   = dr.GetInt32(dr.GetOrdinal("MunicipalityId")),
								MunicipalityName = dr.GetString(dr.GetOrdinal("MunicipalityName")),
								StateId          = dr.GetInt32(dr.GetOrdinal("StateId"))
							});
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener los municipios", ex);
			}

			return municipalities;
		}

		public List<GeographyModel> GetMunicipalityByStateId(int stateId)
		{
			List<GeographyModel> municipalities = new();

			try
			{
				using (SqlConnection sqlConnection = _connectionFactory.CreateConnection())
				{
					if (sqlConnection.State == ConnectionState.Closed)
					{
						sqlConnection.Open();
					}

					using (SqlCommand cmd = new SqlCommand(
						"SELECT MunicipalityId, MunicipalityName FROM dbo.Municipality WHERE StateId = @StateId ORDER BY MunicipalityName",
						sqlConnection))
					{
						cmd.Parameters.AddWithValue("@StateId", stateId);
						using (SqlDataReader dr = cmd.ExecuteReader())
						{
							while (dr.Read())
							{
								municipalities.Add(new GeographyModel
								{
									MunicipalityId = dr.GetInt32(dr.GetOrdinal("MunicipalityId")),
									MunicipalityName = dr.GetString(dr.GetOrdinal("MunicipalityName"))
								});
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener los municipios por estado", ex);
			}

			return municipalities;
		}
	}
}
