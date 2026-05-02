using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
	public class Appointment : IAppointment
	{
		private readonly ISqlConnectionFactory _connectionFactory;
		public Appointment(ISqlConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public int AddOrEdit(AppointmentModel model)
		{
			try
			{
				using SqlConnection connection = _connectionFactory.CreateConnection();
				connection.Open();

				using SqlCommand cmd = new("Appointment_AddOrEdit", connection)
				{
					CommandType = CommandType.StoredProcedure
				};
				model.AppointmentStatusId = model.AppointmentId == 0 ? 1 : model.AppointmentStatusId;
				cmd.Parameters.AddWithValue("@AppointmentId",          model.AppointmentId);
				cmd.Parameters.AddWithValue("@ClinicId",                model.ClinicId);
				cmd.Parameters.AddWithValue("@PatientTypeId",           model.PatientTypeId);
				cmd.Parameters.AddWithValue("@PatientAge",              model.PatientAge);
				cmd.Parameters.AddWithValue("@PatientIdNumber",         model.PatientIdNumber);
				cmd.Parameters.AddWithValue("@ChildGuardianIdNumber",   model.ChildGuardianIdNumber);
				cmd.Parameters.AddWithValue("@ChildGuardianName",       (object?)model.ChildGuardianName ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@RelationshipId",          model.RelationshipId);
				cmd.Parameters.AddWithValue("@PatientName",             (object?)model.PatientName ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@SexId",                   model.SexId);
				cmd.Parameters.AddWithValue("@SpecialtyDoctorId",       model.SpecialtyDoctorId);
				cmd.Parameters.AddWithValue("@AppointmentDate",         model.AppointmentDate);
				cmd.Parameters.AddWithValue("@Symptoms",                (object?)model.Symptoms ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@AppointmentStatusId",     model.AppointmentStatusId);
				cmd.Parameters.AddWithValue("@PatientPhone",            (object?)model.PatientPhone           ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@PatientAddress",          (object?)model.PatientAddress          ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@PatientStateId",          model.PatientStateId == 0 ? (object)DBNull.Value : model.PatientStateId);
				cmd.Parameters.AddWithValue("@PatientBirthDate",        model.PatientBirthDate.HasValue ? (object)model.PatientBirthDate.Value : DBNull.Value);
				cmd.Parameters.AddWithValue("@ChildGuardianPhone",      (object?)model.ChildGuardianPhone      ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@ChildGuardianAddress",    (object?)model.ChildGuardianAddress    ?? DBNull.Value);
				cmd.Parameters.AddWithValue("@ChildGuardianStateId",    model.ChildGuardianStateId == 0 ? (object)DBNull.Value : model.ChildGuardianStateId);
				cmd.Parameters.AddWithValue("@ChildGuardianBirthDate",  model.ChildGuardianBirthDate.HasValue ? (object)model.ChildGuardianBirthDate.Value : DBNull.Value);
				cmd.Parameters.AddWithValue("@ChildGuardianSexId",      model.ChildGuardianSexId == 0 ? (object)DBNull.Value : model.ChildGuardianSexId);
				cmd.Parameters.AddWithValue("@ReasonId",   model.ReasonId == 0 ? (object)DBNull.Value : model.ReasonId);
				cmd.Parameters.Add(new SqlParameter("@PatientId", SqlDbType.Int)
				{
					Value = (model.PatientId.HasValue && model.PatientId.Value > 0)
					        ? (object)model.PatientId.Value
					        : DBNull.Value
				});

				var result = cmd.ExecuteScalar();
				return result != null && result != DBNull.Value ? Convert.ToInt32(result) : model.AppointmentId;
			}
			catch (Exception ex)
			{
				throw new Exception("Error al guardar la cita", ex);
			}
		}
	}
}
