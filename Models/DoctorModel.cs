namespace Medycally.Models
{
    public class DoctorModel
    {
        public int DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public int DoctorIdNumber { get; set; }
        public int SexId { get; set; }
        public string? DoctorMainPhone { get; set; }
        public string? DoctorSecondPhone { get; set; }
        public string? DoctorEmail { get; set; }
        public int StateId { get; set; }
        public string? DoctorAddress { get; set; }
        public int StatusId { get; set; }
        public string? SexName { get; set; }
        public string? StateName { get; set; }
        public string? StatusName { get; set; }
        public string? SpecialtyNames { get; set; }
    }
}
