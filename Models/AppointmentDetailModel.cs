namespace Medycally.Models
{
    public class AppointmentDetailModel
    {
        public int AppointmentId { get; set; }
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public int PatientTypeId { get; set; }

        // Patient / minor
        public string? PatientName { get; set; }
        public int PatientAge { get; set; }
        public int PatientIdNumber { get; set; }
        public string? PatientSexName { get; set; }
        public string? PatientPhone { get; set; }
        public string? PatientAddress { get; set; }
        public DateTime? PatientBirthDate { get; set; }
        public string? PatientStateName { get; set; }

        // Patient IDs (para pre-llenar formulario de registro)
        public int PatientSexId           { get; set; }
        public int PatientStateId         { get; set; }

        // Guardian (solo para menores)
        public int ChildGuardianIdNumber { get; set; }
        public string? ChildGuardianName { get; set; }
        public string? RelationshipName { get; set; }
        public string? ChildGuardianPhone { get; set; }
        public string? ChildGuardianAddress { get; set; }
        public DateTime? ChildGuardianBirthDate { get; set; }
        public int ChildGuardianSexId         { get; set; }
        public string? ChildGuardianSexName { get; set; }
        public int ChildGuardianStateId       { get; set; }
        public string? ChildGuardianStateName { get; set; }

        // Cita
        public int ReasonId { get; set; }
        public string? ReasonName { get; set; }
        public string? SpecialtyName { get; set; }
        public string? DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? AppointmentTime { get; set; }
        public string? Symptoms { get; set; }
        public int AppointmentStatusId { get; set; }
        public string? AppointmentStatusName { get; set; }
        public int? PatientId { get; set; }
    }
}
