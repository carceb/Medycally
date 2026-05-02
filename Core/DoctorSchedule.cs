using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
	public class DoctorSchedule : IDoctorSchedule
	{
		private readonly ISqlConnectionFactory _connectionFactory;
		public DoctorSchedule(ISqlConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public int AddOrEdit(DoctorScheduleModel model)
		{
			throw new NotImplementedException();
		}

		public List<AdminScheduleModel> GetByClinicAndDoctor(int clinicId, int doctorId)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();
				SqlCommand cmd = new("DoctorSchedule_GetByClinicAndDoctor", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				cmd.Parameters.AddWithValue("@ClinicId", clinicId);
				cmd.Parameters.AddWithValue("@DoctorId", doctorId);
				var list = new List<AdminScheduleModel>();
				using SqlDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					list.Add(new AdminScheduleModel
					{
						DoctorScheduleId    = reader.GetInt32(reader.GetOrdinal("DoctorScheduleId")),
						DoctorId            = reader.GetInt32(reader.GetOrdinal("DoctorId")),
						ClinicId            = reader.GetInt32(reader.GetOrdinal("ClinicId")),
						DayOfWeek           = reader.GetByte(reader.GetOrdinal("DayOfWeek")),
						StartTime           = reader.GetString(reader.GetOrdinal("StartTime")),
						EndTime             = reader.GetString(reader.GetOrdinal("EndTime")),
						SlotDurationMinutes = reader.GetInt32(reader.GetOrdinal("SlotDurationMinutes")),
						IsActive            = reader.GetBoolean(reader.GetOrdinal("IsActive"))
					});
				}
				return list;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener los horarios", ex);
			}
		}

		public int SaveSchedule(AdminScheduleModel model)
		{
			using SqlConnection connection = _connectionFactory.CreateConnection();
			connection.Open();
			SqlCommand cmd = new("DoctorSchedule_Save", connection)
			{
				CommandType = CommandType.StoredProcedure
			};
			cmd.Parameters.AddWithValue("@DoctorScheduleId",    model.DoctorScheduleId);
			cmd.Parameters.AddWithValue("@DoctorId",            model.DoctorId);
			cmd.Parameters.AddWithValue("@ClinicId",            model.ClinicId);
			cmd.Parameters.AddWithValue("@DayOfWeek",           (byte)model.DayOfWeek);
			cmd.Parameters.Add(new SqlParameter("@StartTime", SqlDbType.Time) { Value = TimeSpan.Parse(model.StartTime) });
			cmd.Parameters.Add(new SqlParameter("@EndTime",   SqlDbType.Time) { Value = TimeSpan.Parse(model.EndTime)   });
			cmd.Parameters.AddWithValue("@SlotDurationMinutes", model.SlotDurationMinutes);
			cmd.Parameters.AddWithValue("@IsActive",            model.IsActive);
			try
			{
				return Convert.ToInt32(cmd.ExecuteScalar());
			}
			catch (SqlException ex)
			{
				// Surface the SQL message directly so conflict descriptions reach the UI
				throw new Exception(ex.Message);
			}
		}

		public void DeleteSchedule(int doctorScheduleId)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();
				SqlCommand cmd = new("DoctorSchedule_Delete", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				cmd.Parameters.AddWithValue("@DoctorScheduleId", doctorScheduleId);
				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				throw new Exception("Error al eliminar el horario", ex);
			}
		}

		public List<DoctorScheduleModel> GetByClinicIdAndSpecialtyId(int clinicId, int specialtyId)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();

				SqlCommand cmd = new("SELECT * FROM Doctor_GetScheduleByClinicIdAndSpecialtyId WHERE ClinicId = @ClinicId AND SpecialtyId = @SpecialtyId", connection)
				{
					CommandType = CommandType.Text
				};

				cmd.Parameters.AddWithValue("@ClinicId", clinicId);
				cmd.Parameters.AddWithValue("@SpecialtyId", specialtyId);

				List<DoctorScheduleModel> schedules = [];

				using SqlDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					var dayName = reader.GetString(reader.GetOrdinal("DayName"));
					schedules.Add(new DoctorScheduleModel
					{
						ClinicId    = reader.GetInt32(reader.GetOrdinal("ClinicId")),
						DoctorId    = reader.GetInt32(reader.GetOrdinal("DoctorId")),
						SpecialtyId = reader.GetInt32(reader.GetOrdinal("SpecialtyId")),
						SpecialtyDoctorId = reader.GetInt32(reader.GetOrdinal("SpecialtyDoctorId")),
						ClinicName    = reader.IsDBNull(reader.GetOrdinal("ClinicFullName")) ? null : reader.GetString(reader.GetOrdinal("ClinicFullName")),
						SpecialtyName = reader.IsDBNull(reader.GetOrdinal("SpecialtyName"))  ? null : reader.GetString(reader.GetOrdinal("SpecialtyName")),
						DoctorName    = reader.IsDBNull(reader.GetOrdinal("DoctorFullName"))  ? null : reader.GetString(reader.GetOrdinal("DoctorFullName")),
						DayName  = dayName,
						DayOfWeek = dayName switch
						{
							"Domingo"    => 0,
							"Lunes"      => 1,
							"Martes"     => 2,
							"Miércoles"  => 3,
							"Jueves"     => 4,
							"Viernes"    => 5,
							"Sábado"     => 6,
							_            => 0
						},
						StartTime          = TimeOnly.FromTimeSpan(reader.GetTimeSpan(reader.GetOrdinal("StartTime"))),
						EndTime            = TimeOnly.FromTimeSpan(reader.GetTimeSpan(reader.GetOrdinal("EndTime"))),
						StartTimeFormatted = reader.GetString(reader.GetOrdinal("StartTimeFormatted")),
						EndTimeFormatted   = reader.GetString(reader.GetOrdinal("EndTimeFormatted")),
						IsActive           = reader.GetBoolean(reader.GetOrdinal("IsActive"))
					});
				}

				return schedules;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al obtener el horario", ex);
			}
		}
	}
}
