using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
	public class Patient : IPatient
	{
		private readonly ISqlConnectionFactory _connectionFactory;
		public Patient(ISqlConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public int AddOrEdit(PatientModel model)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();
				SqlCommand cmd = new("Patient_AddOrEdit", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				cmd.Parameters.AddWithValue("@PatientId",        model.PatientId);
				cmd.Parameters.Add(new SqlParameter("@PatientIdNumber", SqlDbType.Int)
				{
					Value = model.PatientIdNumber.HasValue ? (object)model.PatientIdNumber.Value : DBNull.Value
				});
				cmd.Parameters.AddWithValue("@PatientName",      (object?)model.PatientName      ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@SexId",            model.SexId);
				cmd.Parameters.Add(new SqlParameter("@PatientBirthdate", SqlDbType.Date)
				{
					Value = model.PatientBirthdate.HasValue ? (object)model.PatientBirthdate.Value : DBNull.Value
				});
				cmd.Parameters.AddWithValue("@PatientAddress",   (object?)model.PatientAddress   ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@MunicipalityId",   model.MunicipalityId);
				cmd.Parameters.AddWithValue("@PatientMainPhone", model.PatientMainPhone == 0 ? (object)DBNull.Value : model.PatientMainPhone);
				cmd.Parameters.Add(new SqlParameter("@IsGuardianOnly", SqlDbType.Bit) { Value = model.IsGuardianOnly });
				var result = cmd.ExecuteScalar();
				return result != null && result != DBNull.Value ? Convert.ToInt32(result) : model.PatientId;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al registrar el paciente", ex);
			}
		}

		public List<PatientModel> GetAll(string? search)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();
				SqlCommand cmd = new("Patient_GetAll", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

				var list = new List<PatientModel>();
				using SqlDataReader dr = cmd.ExecuteReader();
				while (dr.Read())
				{
					var p = new PatientModel();
					MapPatient(dr, p);
					p.Age         = dr.IsDBNull(dr.GetOrdinal("Age"))         ? 0    : dr.GetInt32(dr.GetOrdinal("Age"));
					p.FamilyCount = dr.IsDBNull(dr.GetOrdinal("FamilyCount")) ? 0    : dr.GetInt32(dr.GetOrdinal("FamilyCount"));
					p.HasGuardian = !dr.IsDBNull(dr.GetOrdinal("HasGuardian")) && dr.GetBoolean(dr.GetOrdinal("HasGuardian"));
					list.Add(p);
				}
				return list;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener los pacientes", ex);
			}
		}

		public void Delete(int patientId)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();
				SqlCommand cmd = new("Patient_Delete", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				cmd.Parameters.AddWithValue("@PatientId", patientId);
				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				throw new Exception("Error al eliminar el paciente", ex);
			}
		}

		public List<PatientFamilyMemberModel> GetFamily(int patientId)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();
				SqlCommand cmd = new("Patient_GetFamily", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				cmd.Parameters.AddWithValue("@PatientId", patientId);

				var list = new List<PatientFamilyMemberModel>();
				using SqlDataReader dr = cmd.ExecuteReader();
				while (dr.Read())
				{
					list.Add(new PatientFamilyMemberModel
					{
						Role             = dr.IsDBNull(dr.GetOrdinal("Role"))             ? null : dr.GetString(dr.GetOrdinal("Role")),
						PatientId        = dr.GetInt32(dr.GetOrdinal("PatientId")),
						PatientIdNumber  = dr.IsDBNull(dr.GetOrdinal("PatientIdNumber"))  ? null : dr.GetInt32(dr.GetOrdinal("PatientIdNumber")),
						PatientName      = dr.IsDBNull(dr.GetOrdinal("PatientName"))      ? null : dr.GetString(dr.GetOrdinal("PatientName")),
						SexId            = dr.IsDBNull(dr.GetOrdinal("SexId"))            ? 0    : dr.GetInt32(dr.GetOrdinal("SexId")),
						SexName          = dr.IsDBNull(dr.GetOrdinal("SexName"))          ? null : dr.GetString(dr.GetOrdinal("SexName")),
						RelationshipId   = dr.IsDBNull(dr.GetOrdinal("RelationshipId"))   ? 0    : dr.GetInt32(dr.GetOrdinal("RelationshipId")),
						RelationshipName = dr.IsDBNull(dr.GetOrdinal("RelationshipName")) ? null : dr.GetString(dr.GetOrdinal("RelationshipName")),
						PatientMainPhone = dr.IsDBNull(dr.GetOrdinal("PatientMainPhone")) ? 0    : dr.GetInt64(dr.GetOrdinal("PatientMainPhone")),
						PatientBirthdate = dr.IsDBNull(dr.GetOrdinal("PatientBirthdate")) ? null : dr.GetDateTime(dr.GetOrdinal("PatientBirthdate")),
						PatientAddress   = dr.IsDBNull(dr.GetOrdinal("PatientAddress"))   ? null : dr.GetString(dr.GetOrdinal("PatientAddress")),
						MunicipalityId   = dr.IsDBNull(dr.GetOrdinal("MunicipalityId"))   ? 0    : dr.GetInt32(dr.GetOrdinal("MunicipalityId")),
						MunicipalityName = dr.IsDBNull(dr.GetOrdinal("MunicipalityName")) ? null : dr.GetString(dr.GetOrdinal("MunicipalityName")),
						StateId          = dr.IsDBNull(dr.GetOrdinal("StateId"))          ? 0    : dr.GetInt32(dr.GetOrdinal("StateId")),
						StateName        = dr.IsDBNull(dr.GetOrdinal("StateName"))        ? null : dr.GetString(dr.GetOrdinal("StateName")),
					});
				}
				return list;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener el grupo familiar", ex);
			}
		}

		public void RemoveGuardianLink(int patientId, int guardianPatientId)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();
				SqlCommand cmd = new("PatientGuardian_Delete", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				cmd.Parameters.AddWithValue("@PatientId",         patientId);
				cmd.Parameters.AddWithValue("@GuardianPatientId", guardianPatientId);
				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				throw new Exception("Error al desvincular representante", ex);
			}
		}

		public void LinkGuardian(int patientId, int guardianPatientId, int relationshipId)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();
				SqlCommand cmd = new("PatientGuardian_Save", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				cmd.Parameters.AddWithValue("@PatientId",         patientId);
				cmd.Parameters.AddWithValue("@GuardianPatientId", guardianPatientId);
				cmd.Parameters.AddWithValue("@RelationshipId",    relationshipId);
				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				throw new Exception("Error al vincular representante con paciente", ex);
			}
		}

		public PatientModel? GetById(int patientId)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();
				SqlCommand cmd = new(
					"SELECT PatientId, PatientIdNumber, PatientName, SexId, SexName, PatientBirthdate, " +
					"PatientAddress, MunicipalityId, MunicipalityName, StateId, StateName, PatientMainPhone " +
					"FROM dbo.Patient_GetByIdNumber WHERE PatientId = @PatientId",
					connection)
				{ CommandType = CommandType.Text };
				cmd.Parameters.AddWithValue("@PatientId", patientId);

				using SqlDataReader dr = cmd.ExecuteReader();
				if (!dr.Read()) return null;

				var p = new PatientModel();
				MapPatient(dr, p);
				if (p.PatientBirthdate.HasValue)
				{
					var today = DateTime.Today;
					var birth = p.PatientBirthdate.Value;
					p.Age = today.Year - birth.Year;
					if (birth.Date > today.AddYears(-p.Age)) p.Age--;
				}
				return p;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener el paciente", ex);
			}
		}

		public PatientModel? GetByIdNumber(int patientIdNumber)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();
				SqlCommand cmd = new(
					"SELECT PatientId, PatientIdNumber, PatientName, SexId, SexName, PatientBirthdate, " +
					"PatientAddress, MunicipalityId, MunicipalityName, StateId, StateName, PatientMainPhone " +
					"FROM Patient_GetByIdNumber WHERE PatientIdNumber = @PatientIdNumber",
					connection)
				{
					CommandType = CommandType.Text
				};
				cmd.Parameters.AddWithValue("@PatientIdNumber", patientIdNumber);

				using SqlDataReader dr = cmd.ExecuteReader();
				if (!dr.Read()) return null;

				var p = new PatientModel();
				MapPatient(dr, p);
				return p;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener el paciente", ex);
			}
		}

		private static void MapPatient(SqlDataReader dr, PatientModel p)
		{
			p.PatientId        = dr.GetInt32(dr.GetOrdinal("PatientId"));
			p.PatientIdNumber  = dr.IsDBNull(dr.GetOrdinal("PatientIdNumber"))  ? null : dr.GetInt32(dr.GetOrdinal("PatientIdNumber"));
			p.PatientName      = dr.IsDBNull(dr.GetOrdinal("PatientName"))      ? null : dr.GetString(dr.GetOrdinal("PatientName"));
			p.SexId            = dr.GetInt32(dr.GetOrdinal("SexId"));
			p.SexName          = dr.IsDBNull(dr.GetOrdinal("SexName"))          ? null : dr.GetString(dr.GetOrdinal("SexName"));
			p.PatientBirthdate = dr.IsDBNull(dr.GetOrdinal("PatientBirthdate")) ? null : dr.GetDateTime(dr.GetOrdinal("PatientBirthdate"));
			p.PatientAddress   = dr.IsDBNull(dr.GetOrdinal("PatientAddress"))   ? null : dr.GetString(dr.GetOrdinal("PatientAddress"));
			p.MunicipalityId   = dr.IsDBNull(dr.GetOrdinal("MunicipalityId"))   ? 0    : dr.GetInt32(dr.GetOrdinal("MunicipalityId"));
			p.MunicipalityName = dr.IsDBNull(dr.GetOrdinal("MunicipalityName")) ? null : dr.GetString(dr.GetOrdinal("MunicipalityName"));
			p.StateId          = dr.IsDBNull(dr.GetOrdinal("StateId"))          ? 0    : dr.GetInt32(dr.GetOrdinal("StateId"));
			p.StateName        = dr.IsDBNull(dr.GetOrdinal("StateName"))        ? null : dr.GetString(dr.GetOrdinal("StateName"));
			p.PatientMainPhone = dr.IsDBNull(dr.GetOrdinal("PatientMainPhone")) ? 0    : dr.GetInt64(dr.GetOrdinal("PatientMainPhone"));
		}
	}
}
