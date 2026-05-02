namespace Medycally.Models
{
	public class CommonDataModel
	{
	}

	public class SexModel
	{
		public int SexId { get; set; }
		public string? SexName { get; set; }
		public string? DoctorAbbreviation { get; set; }
	}

	public class RelationshipModel
	{
		public int RelationshipId { get; set; }
		public string? RelationshipName { get; set; }
	}

	public class StatusModel
	{
		public int StatusId { get; set; }
		public string? StatusName { get; set; }
	}
}
