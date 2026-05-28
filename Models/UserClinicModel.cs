namespace Medycally.Models
{
    public class UserClinicModel
    {
        public int ClinicId { get; set; }
        public string? ClinicName { get; set; }
        public string? StateName { get; set; }
        public bool IsAssigned { get; set; }
    }
}
