namespace Medycally.Models
{
	public class ClinicModel
	{
		public int ClinicId { get; set; }
		public string? ClinicRif { get; set; }
		public int ClinicTypeId { get; set; }
		public string? ClinicTypeName { get; set; }
		public int ClinicGroupId { get; set; }
		public int StateId { get; set; }
		public string? MunicipalityName { get; set; }
		public string? ClinicName { get; set; }
		public int MunicipalityId { get; set; }
		public string? ClinicAddress { get; set; }
		public string? ClinicPhones { get; set; }
		public string? StateName { get; set; }
		public string? GoogleMapsUrl { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public string? RepresentativeName { get; set; }
		public string? LandingPage { get; set; }
		public DateTime ClinicDateCreated { get; set; }
		public int StatusId { get; set; }
	}
}
