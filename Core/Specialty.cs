using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
	public class Specialty : ISpecialty
	{
		private readonly ISqlConnectionFactory _connectionFactory;
		public Specialty(ISqlConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public int AddOrEdit(SpecialtyModel model)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();

				SqlCommand cmd = new("Specialty_AddOrEdit", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				cmd.Parameters.AddWithValue("@SpecialtyId",   model.SpecialtyId);
				cmd.Parameters.AddWithValue("@SpecialtyName", model.SpecialtyName ?? string.Empty);

				var result = cmd.ExecuteScalar();
				return result != null && result != DBNull.Value ? Convert.ToInt32(result) : model.SpecialtyId;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al guardar la especialidad", ex);
			}
		}

		public List<SpecialtyModel> GetAll()
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();

				SqlCommand cmd = new("Specialty_GetAll", connection)
				{
					CommandType = CommandType.StoredProcedure
				};

				List<SpecialtyModel> specialties = [];

				using SqlDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					specialties.Add(new SpecialtyModel
					{
						SpecialtyId   = reader.GetInt32(reader.GetOrdinal("SpecialtyId")),
						SpecialtyName = reader.IsDBNull(reader.GetOrdinal("SpecialtyName")) ? null : reader.GetString(reader.GetOrdinal("SpecialtyName")),
					});
				}

				return specialties;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener las especialidades", ex);
			}
		}

		public List<SpecialtyModel> GetActives()
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();

				SqlCommand cmd = new("SELECT SpecialtyId, SpecialtyName FROM Specialty_GetActives", connection)
				{
					CommandType = CommandType.Text
				};

				List<SpecialtyModel> specialties = [];

				using SqlDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					specialties.Add(new SpecialtyModel
					{
						SpecialtyId   = reader.GetInt32(reader.GetOrdinal("SpecialtyId")),
						SpecialtyName = reader.IsDBNull(reader.GetOrdinal("SpecialtyName")) ? null : reader.GetString(reader.GetOrdinal("SpecialtyName")),
					});
				}

				return specialties;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener las especialidades activas", ex);
			}
		}

		public void Delete(int specialtyId)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();

				SqlCommand cmd = new("Specialty_Delete", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				cmd.Parameters.AddWithValue("@SpecialtyId", specialtyId);
				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				throw new Exception("Error al eliminar la especialidad", ex);
			}
		}

		public List<SpecialtyModel> GetByClinicId(int clinicId)
		{
			throw new NotImplementedException();
		}
	}
}
