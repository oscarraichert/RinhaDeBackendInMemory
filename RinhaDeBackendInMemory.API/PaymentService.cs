using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;

namespace RinhaDeBackendInMemory.API
{
    public class PaymentService
    {
        public HttpClient Client { get; }
        private static readonly HttpClientHandler Handler = new HttpClientHandler();
        private readonly IConfiguration Config;
        private readonly ConcurrentBag<Payment> Payments = new();
        private readonly ConcurrentDictionary<Guid, Payment> Unprocessed = new();

        public PaymentService(IConfiguration configuration)
        {
            Client = new HttpClient(Handler)
            {
                Timeout = TimeSpan.FromMilliseconds(500) 
            };
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Config = configuration;
        }

        public async Task HandleProccessPayment(PaymentRequest paymentRequest)
        {
            var payment = new Payment { correlationId = paymentRequest.correlationId, amount = paymentRequest.amount };

            await ProcessPayment(payment, false);
        }

        private async Task ProcessPayment(Payment payment, bool isRetry)
        {
            var response = await Client.PostAsync(Config["PROCESSOR_DEFAULT_URL"] + "/payments", JsonContent.Create(payment));

            if (isRetry)
            {
                Unprocessed.TryRemove(payment.correlationId, out _);
            }

            if (response.IsSuccessStatusCode)
            {
                Payments.Add(payment);
            }
            else
            {
                await ProcessPaymentFallback(payment);
            }
        }

        private async Task ProcessPaymentFallback(Payment payment)
        {
            var response = await Client.PostAsync(Config["PROCESSOR_FALLBACK_URL"] + "/payments", JsonContent.Create(payment));

            if (response.IsSuccessStatusCode)
            {
                payment.processedOnFallback = true;
                Payments.Add(payment);
            }
            else
            {
                Unprocessed[payment.correlationId] = payment;
            }
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


        public async Task RetryUnprocessedLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var tasks = new List<Task>();

                foreach (var up in Unprocessed.ToArray()) 
                {
                    var payment = up.Value;

                    tasks.Add(Task.Run(async () =>
                    {
                        await ProcessPayment(payment, true);
                    }));
                }

                await Task.WhenAll(tasks);

                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            }
        }

    }
}
