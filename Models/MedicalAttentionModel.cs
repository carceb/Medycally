namespace Medycally.Models
{
    public class MedicalAttentionModel
    {
        public int AttentionId { get; set; }
        public int AppointmentId { get; set; }
        public int DoctorId { get; set; }
        public int? PatientId { get; set; }
        public string? DoctorName { get; set; }
        public string? SpecialtyName { get; set; }
        public DateTime AttentionDate { get; set; }
        public string? Diagnosis { get; set; }
        public string? Treatment { get; set; }
        public string? Notes { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? Symptoms { get; set; }
        public int? ReasonId { get; set; }
        public string? ReasonName { get; set; }
        public string? PatientName { get; set; }
    }
}
