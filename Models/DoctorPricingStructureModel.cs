namespace Medycally.Models
{
    public class DoctorPricingStructureModel
    {
        public int DoctorPricingStructureId { get; set; }
        public int DoctorId           { get; set; }
        public int PricingStructureId { get; set; }
        public int StatusId           { get; set; }

        // Enriquecidos para la grilla del modal
        public string? PricingStructureName  { get; set; }
        public double  PricingStructureValue { get; set; }
        public string? ClinicName            { get; set; }
        public string? StatusName            { get; set; }
    }
}
