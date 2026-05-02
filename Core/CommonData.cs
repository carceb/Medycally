using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
	public class CommonData : ICommonData
	{
		private readonly ISqlConnectionFactory _connectionFactory;
		public CommonData(ISqlConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public List<SexModel> GetAll()
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();

				SqlCommand cmd = new("SELECT * FROM Sex ORDER BY SexId", connection)
				{
					CommandType = CommandType.Text
				};

				List<SexModel> sex = [];

				using SqlDataReader dr = cmd.ExecuteReader();
				while (dr.Read())
				{
					sex.Add(new SexModel
					{
						SexId = dr.GetInt32(dr.GetOrdinal("SexId")),
						SexName = dr.GetString(dr.GetOrdinal("SexName")),
						DoctorAbbreviation = dr.GetString(dr.GetOrdinal("DoctorAbbreviation"))
					});
				}

				return sex;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener el sexo", ex);
			}
		}

		public List<RelationshipModel> GetAllRelationship()
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();

				SqlCommand cmd = new("SELECT * FROM Relationship ORDER BY RelationshipId", connection)
				{
					CommandType = CommandType.Text
				};

				List<RelationshipModel> rel = [];

				using SqlDataReader dr = cmd.ExecuteReader();
				while (dr.Read())
				{
					rel.Add(new RelationshipModel
					{
						RelationshipId = dr.GetInt32(dr.GetOrdinal("RelationshipId")),
						RelationshipName = dr.GetString(dr.GetOrdinal("RelationshipName")),
					});
				}

				return rel;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener la relacion", ex);
			}
		}

		public List<StatusModel> GetAllStatuses()
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();

				SqlCommand cmd = new("SELECT * FROM Status ORDER BY StatusId", connection)
				{
					CommandType = CommandType.Text
				};

				List<StatusModel> statuses = [];

				using SqlDataReader dr = cmd.ExecuteReader();
				while (dr.Read())
				{
					statuses.Add(new StatusModel
					{
						StatusId   = dr.GetInt32(dr.GetOrdinal("StatusId")),
						StatusName = dr.GetString(dr.GetOrdinal("StatusName")),
					});
				}

				return statuses;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener los estatus", ex);
			}
		}
	}
}
