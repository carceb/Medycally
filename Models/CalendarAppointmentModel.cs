namespace Medycally.Models
{
    public class CalendarAppointmentModel
    {
        public int AppointmentId { get; set; }
        public string? PatientName { get; set; }
        public int DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? SpecialtyName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? AppointmentTime { get; set; }
        public int AppointmentStatusId { get; set; }
        public string? AppointmentStatusName { get; set; }
    }
}
