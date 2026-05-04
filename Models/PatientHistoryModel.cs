namespace Medycally.Models
{
    public class PatientHistoryModel
    {
        public int     PatientHistoryId       { get; set; }
        public int     PatientId              { get; set; }

        // Antecedentes Médicos Personales
        public bool    HasHypertension        { get; set; }
        public bool    HasDiabetes            { get; set; }
        public bool    HasHeartDisease        { get; set; }
        public bool    HasAsthmaRespiratory   { get; set; }
        public bool    HasThyroidDisease      { get; set; }
        public bool    HasRenalDisease        { get; set; }
        public bool    HasAnxietyDepression   { get; set; }
        public bool    HasCancer              { get; set; }
        public string? CancerDetail           { get; set; }
        public string? PreviousSurgeries      { get; set; }
        public string? RecentHospitalizations { get; set; }

        // Alergias
        public string? DrugAllergies          { get; set; }
        public string? FoodAllergies          { get; set; }
        public string? OtherAllergies         { get; set; }

        // Medicamentos Actuales
        public string? CurrentMedications     { get; set; }

        // Antecedentes Familiares
        public bool    FamilyDiabetes         { get; set; }
        public bool    FamilyCancer           { get; set; }
        public bool    FamilyHeartDisease     { get; set; }
        public bool    FamilyHypertension     { get; set; }
        public string? FamilyOthers           { get; set; }

        // Hábitos y Estilo de Vida — 0=Nunca/Sedentario, 1=Ex/Moderado/Ocasional, 2=Activo/Frecuente
        public int     SmokingStatus          { get; set; }
        public string? SmokingDailyAmount     { get; set; }
        public int     AlcoholStatus          { get; set; }
        public int     PhysicalActivity       { get; set; }
        public bool    DrugUse                { get; set; }

        // Revisión por Sistemas
        public bool    HasWeightLoss          { get; set; }
        public bool    HasFrequentFever       { get; set; }
        public bool    HasHeadache            { get; set; }
        public bool    HasVisionHearingChange { get; set; }
        public bool    HasBreathingDifficulty { get; set; }

        public DateTime LastUpdated           { get; set; }
    }
}
