using RinhaDeBackendInMemory.API;
using System.Collections.Concurrent;

namespace ProcessedPayments.API
{
    public class ProcessedPaymentsService
    {

        private readonly ConcurrentBag<Payment> Payments = new();

        public ProcessedPaymentsService()
        {

        }

        public void AddToPayments(Payment payment)
        {
            Payments.Add(payment);
        }

        public PaymentSummary PaymentSummary(DateTime? from, DateTime? to)
        {
            from ??= DateTime.MinValue;
            to ??= DateTime.MaxValue;

            var payments = Payments.Where(x => x.requestedAt >= from && x.requestedAt <= to).ToList();

            (int Requests, decimal Amount) defaultPayments = (0, 0);
            (int Requests, decimal Amount) fallbackPayments = (0, 0);

            payments.ForEach(payment =>
            {
                _ = payment.processedOnFallback switch
                {
                    true => fallbackPayments = (fallbackPayments.Requests + 1, fallbackPayments.Amount + payment.amount),
                    false => defaultPayments = (defaultPayments.Requests + 1, defaultPayments.Amount + payment.amount),
                };
            });

            var defaultProcessor = new ProcessorSummary
            {
                TotalRequests = defaultPayments.Requests,
                TotalAmount = defaultPayments.Amount
            };

            var fallbackProcessor = new ProcessorSummary
            {
                TotalRequests = fallbackPayments.Requests,
                TotalAmount = fallbackPayments.Amount
            };

            var summary = new PaymentSummary { Default = defaultProcessor, Fallback = fallbackProcessor };

            return summary;
        }
    }
}
