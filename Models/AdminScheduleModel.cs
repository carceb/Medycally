namespace Medycally.Models
{
    public class AdminScheduleModel
    {
        public int DoctorScheduleId { get; set; }
        public int DoctorId { get; set; }
        public int ClinicId { get; set; }
        public int DayOfWeek { get; set; }
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public int SlotDurationMinutes { get; set; } = 20;
        public bool IsActive { get; set; } = true;
    }
}
