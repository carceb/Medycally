namespace Medycally.Models
{
    public class RegisterPatientRequest
    {
        public int           AppointmentId  { get; set; }
        public PatientModel  Patient        { get; set; } = new();
        public PatientModel? Guardian       { get; set; }
        public int           RelationshipId { get; set; }
    }
}
