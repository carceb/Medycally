namespace Medycally.Models
{
	public class AppointmentModel
	{
		public int AppointmentId { get; set; }
		public int ClinicId { get; set; }
		public int PatientTypeId { get; set; }
		public int PatientAge { get; set; }
		public int PatientIdNumber { get; set; }
		public int ChildGuardianIdNumber { get; set; }
		public string? ChildGuardianName { get; set; }
		public int RelationshipId { get; set; }
		public int SexId { get; set; }
		public string? PatientName { get; set; }
		public int SpecialtyDoctorId { get; set; }
		public DateTime AppointmentDate { get; set; }
		public string? Symptoms { get; set; }
		public int AppointmentStatusId { get; set; }
		public string? DoctorName { get; set; }
		public string? SpecialtyName { get; set; }
		public string? AppointmentStatusName { get; set; }
		public int ReasonId { get; set; }
		public int? PatientId { get; set; }
		public string? PatientPhone { get; set; }
		public string? PatientAddress { get; set; }
		public int PatientStateId { get; set; }
		public DateTime? PatientBirthDate { get; set; }
		public string? ChildGuardianPhone { get; set; }
		public string? ChildGuardianAddress { get; set; }
		public int ChildGuardianStateId { get; set; }
		public DateTime? ChildGuardianBirthDate { get; set; }
		public int ChildGuardianSexId { get; set; }
	}
}
