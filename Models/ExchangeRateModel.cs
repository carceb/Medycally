namespace Medycally.Models
{
    public class ExchangeRateModel
    {
        public string   CurrencyCode { get; set; } = "";
        public decimal  Rate         { get; set; }
        public DateTime FetchedAt    { get; set; }
    }
}
