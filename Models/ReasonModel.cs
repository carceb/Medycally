namespace Medycally.Models
{
	public class ReasonModel
	{
		public int    ReasonId    { get; set; }
		public string ReasonName  { get; set; } = string.Empty;
		public int    SpecialtyId { get; set; }
	}
}
