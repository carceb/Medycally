namespace Medycally.Models
{
    public class PatientFamilyMemberModel
    {
        public string?   Role             { get; set; }  // "guardian" | "dependent"
        public int       PatientId        { get; set; }
        public int?      PatientIdNumber  { get; set; }
        public string?   PatientName      { get; set; }
        public int       SexId            { get; set; }
        public string?   SexName          { get; set; }
        public int       RelationshipId   { get; set; }
        public string?   RelationshipName { get; set; }
        public long      PatientMainPhone { get; set; }
        public DateTime? PatientBirthdate { get; set; }
        public string?   PatientAddress   { get; set; }
        public int       MunicipalityId   { get; set; }
        public string?   MunicipalityName { get; set; }
        public int       StateId          { get; set; }
        public string?   StateName        { get; set; }
    }
}
