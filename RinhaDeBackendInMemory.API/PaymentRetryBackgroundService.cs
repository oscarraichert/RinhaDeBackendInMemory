namespace RinhaDeBackendInMemory.API
{
    public class PaymentRetryBackgroundService : BackgroundService
    {
        private readonly PaymentService _paymentService;

        public PaymentRetryBackgroundService(PaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return _paymentService.RetryUnprocessedLoopAsync(stoppingToken);
        }
    }
}
