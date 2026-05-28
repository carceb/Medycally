using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class MedicalAttention : IMedicalAttention
    {
        private readonly ISqlConnectionFactory _db;

        public MedicalAttention(ISqlConnectionFactory db) => _db = db;

        public List<QueueAppointmentModel> GetQueue(int clinicId, int? doctorId, DateTime? date)
        {
            var list = new List<QueueAppointmentModel>();
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("Appointment_GetConfirmedByClinic", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ClinicId", clinicId);
            cmd.Parameters.AddWithValue("@DoctorId", doctorId.HasValue ? (object)doctorId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Date",     date.HasValue      ? (object)date.Value.Date  : DBNull.Value);

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                int idNumOrd    = dr.GetOrdinal("PatientIdNumber");
                int timeOrd     = dr.GetOrdinal("AppointmentTime");
                int reasonOrd   = dr.GetOrdinal("ReasonName");
                int sympOrd     = dr.GetOrdinal("Symptoms");
                int patIdOrd    = dr.GetOrdinal("PatientId");

                list.Add(new QueueAppointmentModel
                {
                    AppointmentId         = dr.GetInt32(dr.GetOrdinal("AppointmentId")),
                    PatientName           = dr.GetString(dr.GetOrdinal("PatientName")),
                    PatientIdNumber       = dr.IsDBNull(idNumOrd)  ? 0    : dr.GetInt32(idNumOrd),
                    PatientAge            = dr.GetInt32(dr.GetOrdinal("PatientAge")),
                    DoctorId              = dr.GetInt32(dr.GetOrdinal("DoctorId")),
                    DoctorName            = dr.GetString(dr.GetOrdinal("DoctorName")),
                    SpecialtyName         = dr.GetString(dr.GetOrdinal("SpecialtyName")),
                    ReasonName            = dr.IsDBNull(reasonOrd) ? null : dr.GetString(reasonOrd),
                    AppointmentDate       = dr.GetDateTime(dr.GetOrdinal("AppointmentDate")),
                    AppointmentTime       = dr.IsDBNull(timeOrd)   ? null : dr.GetString(timeOrd),
                    AppointmentStatusId   = dr.GetInt32(dr.GetOrdinal("AppointmentStatusId")),
                    AppointmentStatusName = dr.GetString(dr.GetOrdinal("AppointmentStatusName")),
                    Symptoms              = dr.IsDBNull(sympOrd)   ? null : dr.GetString(sympOrd),
                    PatientId             = dr.IsDBNull(patIdOrd)  ? null : dr.GetInt32(patIdOrd),
                });
            }
            return list;
        }

        public List<MedicalAttentionModel> GetHistoryByPatient(int patientIdNumber)
        {
            var list = new List<MedicalAttentionModel>();
            if (patientIdNumber <= 0) return list;

            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("MedicalAttention_GetByPatient", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@PatientIdNumber", patientIdNumber);

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                int notesOrd    = dr.GetOrdinal("Notes");
                int sympOrd     = dr.GetOrdinal("Symptoms");
                int chiefOrd    = dr.GetOrdinal("ChiefComplaint");
                int currentOrd  = dr.GetOrdinal("CurrentIlness");
                int labTestsOrd = dr.GetOrdinal("LaboratoryTests");
                int testReqOrd  = dr.GetOrdinal("TestRequisition");

                list.Add(new MedicalAttentionModel
                {
                    AttentionId     = dr.GetInt32(dr.GetOrdinal("AttentionId")),
                    AppointmentId   = dr.GetInt32(dr.GetOrdinal("AppointmentId")),
                    DoctorName      = dr.GetString(dr.GetOrdinal("DoctorName")),
                    SpecialtyName   = dr.GetString(dr.GetOrdinal("SpecialtyName")),
                    AttentionDate   = dr.GetDateTime(dr.GetOrdinal("AttentionDate")),
                    Diagnosis       = dr.GetString(dr.GetOrdinal("Diagnosis")),
                    Treatment       = dr.GetString(dr.GetOrdinal("Treatment")),
                    Notes           = dr.IsDBNull(notesOrd)    ? null : dr.GetString(notesOrd),
                    AppointmentDate = dr.GetDateTime(dr.GetOrdinal("AppointmentDate")),
                    Symptoms        = dr.IsDBNull(sympOrd)     ? null : dr.GetString(sympOrd),
                    ChiefComplaint  = dr.IsDBNull(chiefOrd)    ? null : dr.GetString(chiefOrd),
                    CurrentIlness   = dr.IsDBNull(currentOrd)  ? null : dr.GetString(currentOrd),
                    LaboratoryTests = dr.IsDBNull(labTestsOrd) ? null : dr.GetString(labTestsOrd),
                    TestRequisition = dr.IsDBNull(testReqOrd)  ? null : dr.GetString(testReqOrd),
                });
            }
            return list;
        }

        public List<MedicalAttentionModel> GetHistoryByGuardian(int guardianIdNumber)
        {
            var list = new List<MedicalAttentionModel>();
            if (guardianIdNumber <= 0) return list;

            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("MedicalAttention_GetByGuardian", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ChildGuardianIdNumber", guardianIdNumber);

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                int notesOrd        = dr.GetOrdinal("Notes");
                int sympOrd         = dr.GetOrdinal("Symptoms");
                int patientNameOrd  = dr.GetOrdinal("PatientName");
                int chiefOrd        = dr.GetOrdinal("ChiefComplaint");
                int currentOrd      = dr.GetOrdinal("CurrentIlness");
                int labTestsOrd     = dr.GetOrdinal("LaboratoryTests");
                int testReqOrd      = dr.GetOrdinal("TestRequisition");

                list.Add(new MedicalAttentionModel
                {
                    AttentionId     = dr.GetInt32(dr.GetOrdinal("AttentionId")),
                    AppointmentId   = dr.GetInt32(dr.GetOrdinal("AppointmentId")),
                    DoctorName      = dr.GetString(dr.GetOrdinal("DoctorName")),
                    SpecialtyName   = dr.GetString(dr.GetOrdinal("SpecialtyName")),
                    AttentionDate   = dr.GetDateTime(dr.GetOrdinal("AttentionDate")),
                    Diagnosis       = dr.GetString(dr.GetOrdinal("Diagnosis")),
                    Treatment       = dr.GetString(dr.GetOrdinal("Treatment")),
                    Notes           = dr.IsDBNull(notesOrd)        ? null : dr.GetString(notesOrd),
                    AppointmentDate = dr.GetDateTime(dr.GetOrdinal("AppointmentDate")),
                    Symptoms        = dr.IsDBNull(sympOrd)         ? null : dr.GetString(sympOrd),
                    PatientName     = dr.IsDBNull(patientNameOrd)  ? null : dr.GetString(patientNameOrd),
                    ChiefComplaint  = dr.IsDBNull(chiefOrd)        ? null : dr.GetString(chiefOrd),
                    CurrentIlness   = dr.IsDBNull(currentOrd)      ? null : dr.GetString(currentOrd),
                    LaboratoryTests = dr.IsDBNull(labTestsOrd)     ? null : dr.GetString(labTestsOrd),
                    TestRequisition = dr.IsDBNull(testReqOrd)      ? null : dr.GetString(testReqOrd),
                });
            }
            return list;
        }

        public MedicalAttentionModel? GetByAppointment(int appointmentId)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("MedicalAttention_GetByAppointment", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);

            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                int notesOrd      = dr.GetOrdinal("Notes");
                int sympOrd       = dr.GetOrdinal("Symptoms");
                int reasonIdOrd   = dr.GetOrdinal("ReasonId");
                int reasonNmOrd   = dr.GetOrdinal("ReasonName");
                int chiefOrd      = dr.GetOrdinal("ChiefComplaint");
                int currentOrd    = dr.GetOrdinal("CurrentIlness");
                int labTestsOrd   = dr.GetOrdinal("LaboratoryTests");
                int testReqOrd    = dr.GetOrdinal("TestRequisition");
                return new MedicalAttentionModel
                {
                    AttentionId     = dr.GetInt32(dr.GetOrdinal("AttentionId")),
                    AppointmentId   = dr.GetInt32(dr.GetOrdinal("AppointmentId")),
                    DoctorName      = dr.GetString(dr.GetOrdinal("DoctorName")),
                    AttentionDate   = dr.GetDateTime(dr.GetOrdinal("AttentionDate")),
                    Diagnosis       = dr.GetString(dr.GetOrdinal("Diagnosis")),
                    Treatment       = dr.GetString(dr.GetOrdinal("Treatment")),
                    Notes           = dr.IsDBNull(notesOrd)    ? null : dr.GetString(notesOrd),
                    Symptoms        = dr.IsDBNull(sympOrd)     ? null : dr.GetString(sympOrd),
                    ReasonId        = dr.IsDBNull(reasonIdOrd) ? null : dr.GetInt32(reasonIdOrd),
                    ReasonName     = dr.IsDBNull(reasonNmOrd) ? null : dr.GetString(reasonNmOrd),
                    ChiefComplaint  = dr.IsDBNull(chiefOrd)    ? null : dr.GetString(chiefOrd),
                    CurrentIlness   = dr.IsDBNull(currentOrd)  ? null : dr.GetString(currentOrd),
                    LaboratoryTests = dr.IsDBNull(labTestsOrd) ? null : dr.GetString(labTestsOrd),
                    TestRequisition = dr.IsDBNull(testReqOrd)  ? null : dr.GetString(testReqOrd),
                };
            }
            return null;
        }

        public int Save(MedicalAttentionModel model)
        {
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("MedicalAttention_Save", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@AttentionId",     model.AttentionId);
            cmd.Parameters.AddWithValue("@AppointmentId",   model.AppointmentId);
            cmd.Parameters.AddWithValue("@Diagnosis",       model.Diagnosis ?? string.Empty);
            cmd.Parameters.AddWithValue("@Treatment",       model.Treatment ?? string.Empty);
            cmd.Parameters.AddWithValue("@Notes",           string.IsNullOrWhiteSpace(model.Notes)           ? (object)DBNull.Value : model.Notes);
            cmd.Parameters.AddWithValue("@ChiefComplaint",  string.IsNullOrWhiteSpace(model.ChiefComplaint)  ? (object)DBNull.Value : model.ChiefComplaint);
            cmd.Parameters.AddWithValue("@CurrentIlness",   string.IsNullOrWhiteSpace(model.CurrentIlness)   ? (object)DBNull.Value : model.CurrentIlness);
            cmd.Parameters.AddWithValue("@LaboratoryTests", string.IsNullOrWhiteSpace(model.LaboratoryTests) ? (object)DBNull.Value : model.LaboratoryTests);
            cmd.Parameters.AddWithValue("@TestRequisition", string.IsNullOrWhiteSpace(model.TestRequisition) ? (object)DBNull.Value : model.TestRequisition);

            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : model.AttentionId;
        }

        public List<MedicalAttentionModel> GetAll()
        {
            var list = new List<MedicalAttentionModel>();
            using var conn = _db.CreateConnection();
            conn.Open();
            using var cmd = new SqlCommand("MedicalAttention_GetAll", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                int patientIdOrd       = dr.GetOrdinal("PatientId");
                int sexNameOrd         = dr.GetOrdinal("SexName");
                int stateNameOrd       = dr.GetOrdinal("StateName");
                int patientIdNumberOrd = dr.GetOrdinal("PatientIdNumber");
                int patientAgeOrd      = dr.GetOrdinal("PatientAge");
                int isMinorOrd         = dr.GetOrdinal("IsMinor");

                list.Add(new MedicalAttentionModel
                {
                    AttentionId     = dr.GetInt32(dr.GetOrdinal("AttentionId")),
                    AppointmentId   = dr.GetInt32(dr.GetOrdinal("AppointmentId")),
                    PatientId       = dr.IsDBNull(patientIdOrd) ? null : dr.GetInt32(patientIdOrd),
                    PatientName     = dr.GetString(dr.GetOrdinal("PatientName")),
                    PatientIdNumber = dr.IsDBNull(patientIdNumberOrd) ? 0    : dr.GetInt32(patientIdNumberOrd),
                    PatientAge      = dr.IsDBNull(patientAgeOrd)      ? 0    : dr.GetInt32(patientAgeOrd),
                    SexName         = dr.IsDBNull(sexNameOrd)         ? null : dr.GetString(sexNameOrd),
                    StateName       = dr.IsDBNull(stateNameOrd)       ? null : dr.GetString(stateNameOrd),
                    IsMinor         = !dr.IsDBNull(isMinorOrd)        && dr.GetBoolean(isMinorOrd),
                    DoctorId        = dr.GetInt32(dr.GetOrdinal("DoctorId")),
                    DoctorName      = dr.GetString(dr.GetOrdinal("DoctorName")),
                    SpecialtyName   = dr.GetString(dr.GetOrdinal("SpecialtyName")),
                    AttentionDate   = dr.GetDateTime(dr.GetOrdinal("AttentionDate")),
                });
            }
            return list;
        }
    }
}
