using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RinhaDeBackendInMemory.API;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<PaymentService>();
builder.Services.AddHostedService<PaymentRetryBackgroundService>();

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/alive", () => "Yes");

app.MapPost("/payments", (PaymentRequest payment, PaymentService service) =>
{
    Task.Run(() => service.HandleProccessPayment(payment));
})
.WithName("ProcessPayment");

app.MapGet("payments-summary", (PaymentService service, [FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
{
    return service.PaymentSummary(from, to);
})
.WithName("PaymentsSummary");

app.Run();
