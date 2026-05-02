namespace Medycally.Models
{
    public class DashboardAppointmentModel
    {
        public int AppointmentId { get; set; }
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public string? PatientName { get; set; }
        public string? SpecialtyName { get; set; }
        public string? DoctorName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? AppointmentTime { get; set; }
        public int AppointmentStatusId { get; set; }
        public string? AppointmentStatusName { get; set; }
        public bool IsRegistered { get; set; }
    }
}
