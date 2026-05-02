using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class AppointmentQuery : IAppointmentQuery
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public AppointmentQuery(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public List<AppointmentStatusModel> GetStatuses()
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();
                SqlCommand cmd = new("AppointmentStatus_GetAll", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                var list = new List<AppointmentStatusModel>();
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new AppointmentStatusModel
                    {
                        AppointmentStatusId   = reader.GetInt32(reader.GetOrdinal("AppointmentStatusId")),
                        AppointmentStatusName = reader.IsDBNull(reader.GetOrdinal("AppointmentStatusName")) ? null : reader.GetString(reader.GetOrdinal("AppointmentStatusName"))
                    });
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener los estatus de cita", ex);
            }
        }

        public void UpdateStatus(int appointmentId, int appointmentStatusId)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();
                SqlCommand cmd = new("Appointment_UpdateStatus", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@AppointmentId",       appointmentId);
                cmd.Parameters.AddWithValue("@AppointmentStatusId", appointmentStatusId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al actualizar el estatus de la cita", ex);
            }
        }

        public AppointmentDetailModel? GetById(int appointmentId)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();
                SqlCommand cmd = new("Appointment_GetById", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                using SqlDataReader r = cmd.ExecuteReader();
                if (!r.Read()) return null;

                return new AppointmentDetailModel
                {
                    AppointmentId           = r.GetInt32(r.GetOrdinal("AppointmentId")),
                    ClinicId                = r.GetInt32(r.GetOrdinal("ClinicId")),
                    ClinicName              = r.IsDBNull(r.GetOrdinal("ClinicName"))               ? null : r.GetString(r.GetOrdinal("ClinicName")),
                    PatientTypeId           = r.GetInt32(r.GetOrdinal("PatientTypeId")),
                    PatientName             = r.IsDBNull(r.GetOrdinal("PatientName"))               ? null : r.GetString(r.GetOrdinal("PatientName")),
                    PatientAge              = r.GetInt32(r.GetOrdinal("PatientAge")),
                    PatientIdNumber         = r.GetInt32(r.GetOrdinal("PatientIdNumber")),
                    PatientSexId            = r.IsDBNull(r.GetOrdinal("PatientSexId"))              ? 0    : r.GetInt32(r.GetOrdinal("PatientSexId")),
                    PatientSexName          = r.IsDBNull(r.GetOrdinal("PatientSexName"))            ? null : r.GetString(r.GetOrdinal("PatientSexName")),
                    PatientPhone            = r.IsDBNull(r.GetOrdinal("PatientPhone"))              ? null : r.GetString(r.GetOrdinal("PatientPhone")),
                    PatientAddress          = r.IsDBNull(r.GetOrdinal("PatientAddress"))            ? null : r.GetString(r.GetOrdinal("PatientAddress")),
                    PatientBirthDate        = r.IsDBNull(r.GetOrdinal("PatientBirthDate"))          ? null : r.GetDateTime(r.GetOrdinal("PatientBirthDate")),
                    PatientStateId          = r.IsDBNull(r.GetOrdinal("PatientStateId"))            ? 0    : r.GetInt32(r.GetOrdinal("PatientStateId")),
                    PatientStateName        = r.IsDBNull(r.GetOrdinal("PatientStateName"))          ? null : r.GetString(r.GetOrdinal("PatientStateName")),
                    ChildGuardianIdNumber   = r.GetInt32(r.GetOrdinal("ChildGuardianIdNumber")),
                    ChildGuardianName       = r.IsDBNull(r.GetOrdinal("ChildGuardianName"))         ? null : r.GetString(r.GetOrdinal("ChildGuardianName")),
                    RelationshipName        = r.IsDBNull(r.GetOrdinal("RelationshipName"))          ? null : r.GetString(r.GetOrdinal("RelationshipName")),
                    ChildGuardianPhone      = r.IsDBNull(r.GetOrdinal("ChildGuardianPhone"))        ? null : r.GetString(r.GetOrdinal("ChildGuardianPhone")),
                    ChildGuardianAddress    = r.IsDBNull(r.GetOrdinal("ChildGuardianAddress"))      ? null : r.GetString(r.GetOrdinal("ChildGuardianAddress")),
                    ChildGuardianBirthDate  = r.IsDBNull(r.GetOrdinal("ChildGuardianBirthDate"))    ? null : r.GetDateTime(r.GetOrdinal("ChildGuardianBirthDate")),
                    ChildGuardianSexId      = r.IsDBNull(r.GetOrdinal("ChildGuardianSexId"))        ? 0    : r.GetInt32(r.GetOrdinal("ChildGuardianSexId")),
                    ChildGuardianSexName    = r.IsDBNull(r.GetOrdinal("ChildGuardianSexName"))      ? null : r.GetString(r.GetOrdinal("ChildGuardianSexName")),
                    ChildGuardianStateId    = r.IsDBNull(r.GetOrdinal("ChildGuardianStateId"))      ? 0    : r.GetInt32(r.GetOrdinal("ChildGuardianStateId")),
                    ChildGuardianStateName  = r.IsDBNull(r.GetOrdinal("ChildGuardianStateName"))    ? null : r.GetString(r.GetOrdinal("ChildGuardianStateName")),
                    ReasonId                = r.IsDBNull(r.GetOrdinal("ReasonId"))                  ? 0    : r.GetInt32(r.GetOrdinal("ReasonId")),
                    ReasonName              = r.IsDBNull(r.GetOrdinal("ReasonName"))                ? null : r.GetString(r.GetOrdinal("ReasonName")),
                    SpecialtyName           = r.IsDBNull(r.GetOrdinal("SpecialtyName"))             ? null : r.GetString(r.GetOrdinal("SpecialtyName")),
                    DoctorName              = r.IsDBNull(r.GetOrdinal("DoctorName"))                ? null : r.GetString(r.GetOrdinal("DoctorName")),
                    AppointmentDate         = r.GetDateTime(r.GetOrdinal("AppointmentDate")),
                    AppointmentTime         = r.IsDBNull(r.GetOrdinal("AppointmentTime"))           ? null : r.GetString(r.GetOrdinal("AppointmentTime")),
                    Symptoms                = r.IsDBNull(r.GetOrdinal("Symptoms"))                  ? null : r.GetString(r.GetOrdinal("Symptoms")),
                    AppointmentStatusId     = r.GetInt32(r.GetOrdinal("AppointmentStatusId")),
                    AppointmentStatusName   = r.IsDBNull(r.GetOrdinal("AppointmentStatusName"))     ? null : r.GetString(r.GetOrdinal("AppointmentStatusName")),
                    PatientId               = r.IsDBNull(r.GetOrdinal("PatientId"))                 ? null : r.GetInt32(r.GetOrdinal("PatientId")),
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener el detalle de la cita", ex);
            }
        }

        public void Delete(int appointmentId)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();
                SqlCommand cmd = new("Appointment_Delete", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al eliminar la cita", ex);
            }
        }

        public void SetPatientId(int appointmentId, int patientId)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();
                SqlCommand cmd = new("Appointment_SetPatientId", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
                cmd.Parameters.AddWithValue("@PatientId",     patientId);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al vincular paciente con la cita", ex);
            }
        }

        public List<DashboardAppointmentModel> GetByClinic(int clinicId, DateTime? date)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();

                SqlCommand cmd = new("Appointment_GetByClinic", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClinicId", clinicId);
                cmd.Parameters.Add(new SqlParameter("@Date", SqlDbType.Date)
                {
                    Value = date.HasValue ? (object)date.Value.Date : DBNull.Value
                });

                var list = new List<DashboardAppointmentModel>();
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new DashboardAppointmentModel
                    {
                        AppointmentId         = reader.GetInt32(reader.GetOrdinal("AppointmentId")),
                        ClinicId              = reader.GetInt32(reader.GetOrdinal("ClinicId")),
                        ClinicName            = reader.IsDBNull(reader.GetOrdinal("ClinicName"))            ? null : reader.GetString(reader.GetOrdinal("ClinicName")),
                        PatientName           = reader.IsDBNull(reader.GetOrdinal("PatientName"))           ? null : reader.GetString(reader.GetOrdinal("PatientName")),
                        SpecialtyName         = reader.IsDBNull(reader.GetOrdinal("SpecialtyName"))         ? null : reader.GetString(reader.GetOrdinal("SpecialtyName")),
                        DoctorName            = reader.IsDBNull(reader.GetOrdinal("DoctorName"))            ? null : reader.GetString(reader.GetOrdinal("DoctorName")),
                        AppointmentDate       = reader.GetDateTime(reader.GetOrdinal("AppointmentDate")),
                        AppointmentTime       = reader.IsDBNull(reader.GetOrdinal("AppointmentTime"))       ? null : reader.GetString(reader.GetOrdinal("AppointmentTime")),
                        AppointmentStatusId   = reader.GetInt32(reader.GetOrdinal("AppointmentStatusId")),
                        AppointmentStatusName = reader.IsDBNull(reader.GetOrdinal("AppointmentStatusName")) ? null : reader.GetString(reader.GetOrdinal("AppointmentStatusName")),
                        IsRegistered          = reader.GetBoolean(reader.GetOrdinal("IsRegistered")),
                    });
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener las citas", ex);
            }
        }
    }
}
