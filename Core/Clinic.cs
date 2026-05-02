using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class Clinic : IClinic
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public Clinic(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public int AddOrEdit(ClinicModel model)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                SqlCommand cmd = new("Clinic_AddOrEdit", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@ClinicId", model.ClinicId);
                cmd.Parameters.AddWithValue("@ClinicRif", (object?)model.ClinicRif ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ClinicTypeId", model.ClinicTypeId);
                cmd.Parameters.AddWithValue("@ClinicGroupId", model.ClinicGroupId);
                cmd.Parameters.AddWithValue("@ClinicName", (object?)model.ClinicName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MunicipalityId", model.MunicipalityId);
                cmd.Parameters.AddWithValue("@ClinicAddress", (object?)model.ClinicAddress ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ClinicPhones", (object?)model.ClinicPhones ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GoogleMapsUrl", (object?)model.GoogleMapsUrl ?? DBNull.Value);
                cmd.Parameters.Add(new SqlParameter("@Latitude",  SqlDbType.Float) { Value = model.Latitude  });
                cmd.Parameters.Add(new SqlParameter("@Longitude", SqlDbType.Float) { Value = model.Longitude });
                cmd.Parameters.AddWithValue("@RepresentativeName", (object?)model.RepresentativeName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LandingPage", (object?)model.LandingPage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StatusId", model.StatusId);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                throw new Exception("Error al añadir o editar la clinica", ex);
            }
        }

        public List<ClinicModel> GetAll()
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                SqlCommand cmd = new("Clinic_GetAll", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                List<ClinicModel> clinics = [];

                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    clinics.Add(MapClinic(reader));
                }

                return clinics;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener las clinicas", ex);
            }
        }

        public ClinicModel GetById(int clinicId)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                SqlCommand cmd = new("Clinic_GetById", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@ClinicId", clinicId);

                using SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return MapClinic(reader);
                }

                return null!;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener la clinica", ex);
            }
        }

		public List<ClinicModel> GetBySpecialtyId(int specialtyId)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();

				SqlCommand cmd = new("SELECT * FROM Clinic_GetBySpecialtyId WHERE SpecialtyId = @SpecialtyId", connection)
				{
					CommandType = CommandType.Text
				};

				cmd.Parameters.AddWithValue("@SpecialtyId", specialtyId);

				List<ClinicModel> clinics = [];

				using SqlDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					clinics.Add(new ClinicModel
					{
						ClinicId       = reader.GetInt32(reader.GetOrdinal("ClinicId")),
						ClinicRif      = reader.IsDBNull(reader.GetOrdinal("ClinicRif"))      ? null : reader.GetString(reader.GetOrdinal("ClinicRif")),
						ClinicTypeId   = reader.IsDBNull(reader.GetOrdinal("ClinicTypeId"))   ? 0    : reader.GetInt32(reader.GetOrdinal("ClinicTypeId")),
						//ClinicGroupId = reader.IsDBNull(reader.GetOrdinal("ClinicGroupId")) ? 0 : reader.GetInt32(reader.GetOrdinal("ClinicGroupId")),
						ClinicName     = reader.IsDBNull(reader.GetOrdinal("ClinicFullName")) ? null : reader.GetString(reader.GetOrdinal("ClinicFullName")),
						MunicipalityId = reader.IsDBNull(reader.GetOrdinal("MunicipalityId")) ? 0    : reader.GetInt32(reader.GetOrdinal("MunicipalityId")),
						ClinicAddress  = reader.IsDBNull(reader.GetOrdinal("ClinicAddress"))  ? null : reader.GetString(reader.GetOrdinal("ClinicAddress")),
						ClinicPhones   = reader.IsDBNull(reader.GetOrdinal("ClinicPhones"))   ? null : reader.GetString(reader.GetOrdinal("ClinicPhones")),
						StateName      = reader.IsDBNull(reader.GetOrdinal("StateName"))      ? null : reader.GetString(reader.GetOrdinal("StateName")),
						GoogleMapsUrl  = reader.IsDBNull(reader.GetOrdinal("GoogleMapsUrl"))  ? null : reader.GetString(reader.GetOrdinal("GoogleMapsUrl")),
						Latitude       = reader.IsDBNull(reader.GetOrdinal("Latitude"))       ? 0    : reader.GetDouble(reader.GetOrdinal("Latitude")),
						Longitude      = reader.IsDBNull(reader.GetOrdinal("Longitude"))      ? 0    : reader.GetDouble(reader.GetOrdinal("Longitude")),
					});
				}

				return clinics;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener las clinicas por especialidad", ex);
			}
		}

		private static ClinicModel MapClinic(SqlDataReader reader)
        {
            return new ClinicModel
            {
                ClinicId          = reader.GetInt32(reader.GetOrdinal("ClinicId")),
                ClinicRif         = reader.IsDBNull(reader.GetOrdinal("ClinicRif"))         ? null : reader.GetString(reader.GetOrdinal("ClinicRif")),
                ClinicTypeId      = reader.IsDBNull(reader.GetOrdinal("ClinicTypeId"))      ? 0    : reader.GetInt32(reader.GetOrdinal("ClinicTypeId")),
                ClinicTypeName    = reader.IsDBNull(reader.GetOrdinal("ClinicTypeName"))    ? null : reader.GetString(reader.GetOrdinal("ClinicTypeName")),
                ClinicGroupId     = reader.IsDBNull(reader.GetOrdinal("ClinicGroupId"))     ? 0    : reader.GetInt32(reader.GetOrdinal("ClinicGroupId")),
                ClinicName        = reader.IsDBNull(reader.GetOrdinal("ClinicName"))        ? null : reader.GetString(reader.GetOrdinal("ClinicName")),
                MunicipalityId    = reader.IsDBNull(reader.GetOrdinal("MunicipalityId"))    ? 0    : reader.GetInt32(reader.GetOrdinal("MunicipalityId")),
                MunicipalityName  = reader.IsDBNull(reader.GetOrdinal("MunicipalityName"))  ? null : reader.GetString(reader.GetOrdinal("MunicipalityName")),
                StateId           = reader.IsDBNull(reader.GetOrdinal("StateId"))           ? 0    : reader.GetInt32(reader.GetOrdinal("StateId")),
                StateName         = reader.IsDBNull(reader.GetOrdinal("StateName"))         ? null : reader.GetString(reader.GetOrdinal("StateName")),
                ClinicAddress     = reader.IsDBNull(reader.GetOrdinal("ClinicAddress"))     ? null : reader.GetString(reader.GetOrdinal("ClinicAddress")),
                ClinicPhones      = reader.IsDBNull(reader.GetOrdinal("ClinicPhones"))      ? null : reader.GetString(reader.GetOrdinal("ClinicPhones")),
                GoogleMapsUrl     = reader.IsDBNull(reader.GetOrdinal("GoogleMapsUrl"))     ? null : reader.GetString(reader.GetOrdinal("GoogleMapsUrl")),
                Latitude          = reader.IsDBNull(reader.GetOrdinal("Latitude"))          ? 0    : reader.GetDouble(reader.GetOrdinal("Latitude")),
                Longitude         = reader.IsDBNull(reader.GetOrdinal("Longitude"))         ? 0    : reader.GetDouble(reader.GetOrdinal("Longitude")),
                RepresentativeName= reader.IsDBNull(reader.GetOrdinal("RepresentativeName"))? null : reader.GetString(reader.GetOrdinal("RepresentativeName")),
                LandingPage       = reader.IsDBNull(reader.GetOrdinal("LandingPage"))       ? null : reader.GetString(reader.GetOrdinal("LandingPage")),
                ClinicDateCreated = reader.IsDBNull(reader.GetOrdinal("ClinicDateCreated")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("ClinicDateCreated")),
                StatusId          = reader.IsDBNull(reader.GetOrdinal("StatusId"))          ? 0    : reader.GetInt32(reader.GetOrdinal("StatusId"))
            };
        }


		public void Delete(int clinicId)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();

				SqlCommand cmd = new("Clinic_Delete", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				cmd.Parameters.AddWithValue("@ClinicId", clinicId);
				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				throw new Exception("Error al eliminar la clínica", ex);
			}
		}

		public List<ClinicDoctorModel> GetDoctors(int clinicId)
		{
			var list = new List<ClinicDoctorModel>();
			using var conn = _connectionFactory.CreateConnection();
			conn.Open();
			using var cmd = new SqlCommand("ClinicDoctor_GetByClinicId", conn) { CommandType = CommandType.StoredProcedure };
			cmd.Parameters.AddWithValue("@ClinicId", clinicId);
			using var r = cmd.ExecuteReader();
			while (r.Read())
			{
				list.Add(new ClinicDoctorModel
				{
					DoctorId   = Convert.ToInt32(r["DoctorId"]),
					DoctorName = r["DoctorName"] == DBNull.Value ? null : r["DoctorName"].ToString(),
					IsAssigned = r["IsAssigned"] != DBNull.Value && Convert.ToBoolean(r["IsAssigned"]),
				});
			}
			return list;
		}

		public void SaveDoctors(int clinicId, List<int> doctorIds)
		{
			using var conn = _connectionFactory.CreateConnection();
			conn.Open();
			using var cmd = new SqlCommand("ClinicDoctor_Save", conn) { CommandType = CommandType.StoredProcedure };
			cmd.Parameters.AddWithValue("@ClinicId",   clinicId);
			cmd.Parameters.AddWithValue("@DoctorIds",  doctorIds.Count > 0
				? (object)string.Join(",", doctorIds)
				: DBNull.Value);
			cmd.ExecuteNonQuery();
		}
	}
}
