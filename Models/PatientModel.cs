namespace Medycally.Models
{
    public class PatientModel
    {
        public int     PatientId        { get; set; }
        public int?    PatientIdNumber  { get; set; }  // NULL para menores sin cédula
        public string? PatientName      { get; set; }
        public int     SexId            { get; set; }
        public string? SexName          { get; set; }
        public DateTime? PatientBirthdate { get; set; }
        public string? PatientAddress   { get; set; }
        public int     MunicipalityId   { get; set; }
        public string? MunicipalityName { get; set; }
        public int     StateId          { get; set; }
        public string? StateName        { get; set; }
        public long    PatientMainPhone { get; set; }
        public int     Age              { get; set; }
        public int     FamilyCount      { get; set; }
        public bool    IsGuardianOnly   { get; set; }
    }
}
