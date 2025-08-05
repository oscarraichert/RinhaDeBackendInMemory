using System.Collections.Concurrent;
using System.Net.Http.Headers;

namespace RinhaDeBackendInMemory.API
{
    public class PaymentService
    {
        public HttpClient Client { get; }
        public UnixSocketHttpClient UnixClient { get; set; }
        private static readonly HttpClientHandler Handler = new HttpClientHandler();
        private readonly IConfiguration Config;
        private readonly ConcurrentDictionary<Guid, Payment> Unprocessed = new();

        public PaymentService(IConfiguration configuration, UnixSocketHttpClient unixClient)
        {
            Client = new HttpClient(Handler)
            {
                Timeout = TimeSpan.FromMilliseconds(500) 
            };

            UnixClient = unixClient;
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
                await AddToPayments(payment);
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
                await AddToPayments(payment);
            }
            else
            {
                Unprocessed[payment.correlationId] = payment;
            }
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

        public async Task<PaymentSummary> PaymentSummary(DateTime? from, DateTime? to)
        {
            var result = await UnixClient.GetAsync("/payments-summary").Result.Content.ReadFromJsonAsync<PaymentSummary>();

            return result!;
        }

        public async Task AddToPayments(Payment payment)
        {
            await UnixClient.PostAsJsonAsync("/payments-summary", payment);
        }
    }
}
