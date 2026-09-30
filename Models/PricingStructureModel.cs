namespace Medycally.Models
{
    public class PricingStructureModel
    {
        public int     PricingStructureId    { get; set; }
        public int     ClinicId              { get; set; }
        public string  PricingStructureName  { get; set; } = string.Empty;
        public double  PricingStructureValue { get; set; }
        public string? ClinicName            { get; set; }
    }
}
