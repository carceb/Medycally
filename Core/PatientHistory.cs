using Medycally.Core.Data;
using Medycally.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Medycally.Core
{
    public class PatientHistory : IPatientHistory
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public PatientHistory(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PatientHistoryModel? GetByPatientId(int patientId)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();
                SqlCommand cmd = new("PatientHistory_GetByPatientId", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@PatientId", patientId);

                using SqlDataReader dr = cmd.ExecuteReader();
                if (!dr.Read()) return null;

                return MapHistory(dr);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener los antecedentes del paciente", ex);
            }
        }

        public void Save(PatientHistoryModel m, int updatedByUserId)
        {
            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection();
                connection.Open();
                SqlCommand cmd = new("PatientHistory_Save", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@PatientId",              m.PatientId);
                cmd.Parameters.AddWithValue("@HasHypertension",        m.HasHypertension);
                cmd.Parameters.AddWithValue("@HasDiabetes",            m.HasDiabetes);
                cmd.Parameters.AddWithValue("@HasHeartDisease",        m.HasHeartDisease);
                cmd.Parameters.AddWithValue("@HasAsthmaRespiratory",   m.HasAsthmaRespiratory);
                cmd.Parameters.AddWithValue("@HasThyroidDisease",      m.HasThyroidDisease);
                cmd.Parameters.AddWithValue("@HasRenalDisease",        m.HasRenalDisease);
                cmd.Parameters.AddWithValue("@HasAnxietyDepression",   m.HasAnxietyDepression);
                cmd.Parameters.AddWithValue("@HasCancer",              m.HasCancer);
                cmd.Parameters.AddWithValue("@CancerDetail",           (object?)m.CancerDetail           ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PreviousSurgeries",      (object?)m.PreviousSurgeries      ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RecentHospitalizations", (object?)m.RecentHospitalizations ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DrugAllergies",          (object?)m.DrugAllergies          ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FoodAllergies",          (object?)m.FoodAllergies          ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OtherAllergies",         (object?)m.OtherAllergies         ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CurrentMedications",     (object?)m.CurrentMedications     ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FamilyDiabetes",         m.FamilyDiabetes);
                cmd.Parameters.AddWithValue("@FamilyCancer",           m.FamilyCancer);
                cmd.Parameters.AddWithValue("@FamilyHeartDisease",     m.FamilyHeartDisease);
                cmd.Parameters.AddWithValue("@FamilyHypertension",     m.FamilyHypertension);
                cmd.Parameters.AddWithValue("@FamilyOthers",           (object?)m.FamilyOthers           ?? DBNull.Value);
                cmd.Parameters.Add(new SqlParameter("@SmokingStatus",    SqlDbType.TinyInt) { Value = m.SmokingStatus });
                cmd.Parameters.AddWithValue("@SmokingDailyAmount",     (object?)m.SmokingDailyAmount     ?? DBNull.Value);
                cmd.Parameters.Add(new SqlParameter("@AlcoholStatus",    SqlDbType.TinyInt) { Value = m.AlcoholStatus });
                cmd.Parameters.Add(new SqlParameter("@PhysicalActivity", SqlDbType.TinyInt) { Value = m.PhysicalActivity });
                cmd.Parameters.AddWithValue("@DrugUse",                m.DrugUse);
                cmd.Parameters.AddWithValue("@HasWeightLoss",          m.HasWeightLoss);
                cmd.Parameters.AddWithValue("@HasFrequentFever",       m.HasFrequentFever);
                cmd.Parameters.AddWithValue("@HasHeadache",            m.HasHeadache);
                cmd.Parameters.AddWithValue("@HasVisionHearingChange", m.HasVisionHearingChange);
                cmd.Parameters.AddWithValue("@HasBreathingDifficulty", m.HasBreathingDifficulty);
                cmd.Parameters.AddWithValue("@UpdatedByUserId",        updatedByUserId);

                cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al guardar los antecedentes del paciente", ex);
            }
        }

        private static PatientHistoryModel MapHistory(SqlDataReader dr)
        {
            return new PatientHistoryModel
            {
                PatientHistoryId       = dr.GetInt32(dr.GetOrdinal("PatientHistoryId")),
                PatientId              = dr.GetInt32(dr.GetOrdinal("PatientId")),
                HasHypertension        = dr.GetBoolean(dr.GetOrdinal("HasHypertension")),
                HasDiabetes            = dr.GetBoolean(dr.GetOrdinal("HasDiabetes")),
                HasHeartDisease        = dr.GetBoolean(dr.GetOrdinal("HasHeartDisease")),
                HasAsthmaRespiratory   = dr.GetBoolean(dr.GetOrdinal("HasAsthmaRespiratory")),
                HasThyroidDisease      = dr.GetBoolean(dr.GetOrdinal("HasThyroidDisease")),
                HasRenalDisease        = dr.GetBoolean(dr.GetOrdinal("HasRenalDisease")),
                HasAnxietyDepression   = dr.GetBoolean(dr.GetOrdinal("HasAnxietyDepression")),
                HasCancer              = dr.GetBoolean(dr.GetOrdinal("HasCancer")),
                CancerDetail           = dr.IsDBNull(dr.GetOrdinal("CancerDetail"))           ? null : dr.GetString(dr.GetOrdinal("CancerDetail")),
                PreviousSurgeries      = dr.IsDBNull(dr.GetOrdinal("PreviousSurgeries"))      ? null : dr.GetString(dr.GetOrdinal("PreviousSurgeries")),
                RecentHospitalizations = dr.IsDBNull(dr.GetOrdinal("RecentHospitalizations")) ? null : dr.GetString(dr.GetOrdinal("RecentHospitalizations")),
                DrugAllergies          = dr.IsDBNull(dr.GetOrdinal("DrugAllergies"))          ? null : dr.GetString(dr.GetOrdinal("DrugAllergies")),
                FoodAllergies          = dr.IsDBNull(dr.GetOrdinal("FoodAllergies"))          ? null : dr.GetString(dr.GetOrdinal("FoodAllergies")),
                OtherAllergies         = dr.IsDBNull(dr.GetOrdinal("OtherAllergies"))         ? null : dr.GetString(dr.GetOrdinal("OtherAllergies")),
                CurrentMedications     = dr.IsDBNull(dr.GetOrdinal("CurrentMedications"))     ? null : dr.GetString(dr.GetOrdinal("CurrentMedications")),
                FamilyDiabetes         = dr.GetBoolean(dr.GetOrdinal("FamilyDiabetes")),
                FamilyCancer           = dr.GetBoolean(dr.GetOrdinal("FamilyCancer")),
                FamilyHeartDisease     = dr.GetBoolean(dr.GetOrdinal("FamilyHeartDisease")),
                FamilyHypertension     = dr.GetBoolean(dr.GetOrdinal("FamilyHypertension")),
                FamilyOthers           = dr.IsDBNull(dr.GetOrdinal("FamilyOthers"))           ? null : dr.GetString(dr.GetOrdinal("FamilyOthers")),
                SmokingStatus          = dr.GetByte(dr.GetOrdinal("SmokingStatus")),
                SmokingDailyAmount     = dr.IsDBNull(dr.GetOrdinal("SmokingDailyAmount"))     ? null : dr.GetString(dr.GetOrdinal("SmokingDailyAmount")),
                AlcoholStatus          = dr.GetByte(dr.GetOrdinal("AlcoholStatus")),
                PhysicalActivity       = dr.GetByte(dr.GetOrdinal("PhysicalActivity")),
                DrugUse                = dr.GetBoolean(dr.GetOrdinal("DrugUse")),
                HasWeightLoss          = dr.GetBoolean(dr.GetOrdinal("HasWeightLoss")),
                HasFrequentFever       = dr.GetBoolean(dr.GetOrdinal("HasFrequentFever")),
                HasHeadache            = dr.GetBoolean(dr.GetOrdinal("HasHeadache")),
                HasVisionHearingChange = dr.GetBoolean(dr.GetOrdinal("HasVisionHearingChange")),
                HasBreathingDifficulty = dr.GetBoolean(dr.GetOrdinal("HasBreathingDifficulty")),
                LastUpdated            = dr.GetDateTime(dr.GetOrdinal("LastUpdated")),
            };
        }
    }
}
