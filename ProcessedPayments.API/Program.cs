using ProcessedPayments.API;
using RinhaDeBackendInMemory.API;

var builder = WebApplication.CreateBuilder();
builder.Services.AddSingleton<ProcessedPaymentsService>();

const string UnixSocketPath = "/tmp/processed_payments.sock";

if (File.Exists(UnixSocketPath))
{
    File.Delete(UnixSocketPath);
}

builder.WebHost.ConfigureKestrel(options => options.ListenUnixSocket(UnixSocketPath));

var app = builder.Build();

app.MapGet("/payments-summary", (ProcessedPaymentsService service, DateTime? from, DateTime? to) =>
{
    return service.PaymentSummary(from, to);
});

app.MapPost("/add-payment", (ProcessedPaymentsService service, Payment payment) => 
{
    service.AddToPayments(payment);
});

app.Run();
