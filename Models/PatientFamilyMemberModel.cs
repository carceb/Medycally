namespace Medycally.Models
{
    public class PatientFamilyMemberModel
    {
        public string? Role             { get; set; }  // "guardian" | "dependent"
        public int     PatientId        { get; set; }
        public int?    PatientIdNumber  { get; set; }
        public string? PatientName      { get; set; }
        public string? SexName          { get; set; }
        public string? RelationshipName { get; set; }
    }
}
