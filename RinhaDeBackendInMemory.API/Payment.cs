namespace RinhaDeBackendInMemory.API
{
    public class Payment
    {
        public Guid correlationId { get; set; }
        public decimal amount { get; set; }
        public DateTime requestedAt { get; set; } = DateTime.UtcNow;
        public bool processedOnFallback { get; set; }
    }

    public record PaymentRequest(Guid correlationId, decimal amount);
}
