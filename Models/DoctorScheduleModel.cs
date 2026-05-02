namespace Medycally.Models
{
	public class DoctorScheduleModel
	{
		public int DoctorId { get; set; }
		public int ClinicId { get; set; }
		public int SpecialtyId { get; set; }
		public int SpecialtyDoctorId { get; set; }
		public string? ClinicName { get; set; }
		public string? SpecialtyName { get; set; }
		public string? DoctorName { get; set; }
		public string? DoctorMainPhone { get; set; }
		public int DayOfWeek { get; set; }
		public TimeOnly StartTime { get; set; }
		public TimeOnly EndTime { get; set; }
		public string? DayName { get; set; }
		public string? StartTimeFormatted { get; set; }
		public string? EndTimeFormatted { get; set; }
		public bool IsActive { get; set; }
	}
}
