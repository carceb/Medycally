namespace Medycally.Models
{
    public class ClinicSpecialtyFeeModel
    {
        public int       ClinicSpecialtyFeeId { get; set; }
        public int       ClinicId            { get; set; }
        public int       SpecialtyId         { get; set; }
        public string?   SpecialtyName       { get; set; }
        public decimal   FeeUSD              { get; set; }
        public decimal   FeeVES              { get; set; }
        public decimal   ExchangeRateUsed    { get; set; }
        public DateTime? UpdatedAt           { get; set; }
    }
}
