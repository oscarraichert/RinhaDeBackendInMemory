var builder = WebApplication.CreateBuilder();

const string UnixSocketPath = "/tmp/processed_payments.sock";

if (File.Exists(UnixSocketPath))
{
    File.Delete(UnixSocketPath);
}

builder.WebHost.ConfigureKestrel(options => options.ListenUnixSocket(UnixSocketPath));

var app = builder.Build();

app.MapGet("/hello", () => "Hello, Unix Socket");

app.Run();
