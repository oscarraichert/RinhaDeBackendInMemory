namespace RinhaDeBackendInMemory.API
{
    public class PaymentSummary
    {
        public required ProcessorSummary Default { get; set; }
        public required ProcessorSummary Fallback { get; set; }
    }

    public class ProcessorSummary
    {
        public required int TotalRequests { get; set; }
        public required decimal TotalAmount { get; set; }
    }
}
