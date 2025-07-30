using System.Net.Sockets;

public class UnixSocketHttpClient : HttpClient
{
    public UnixSocketHttpClient(HttpMessageHandler handler) : base(handler)
    {
    }

    public static UnixSocketHttpClient Create(string socketPath)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var endpoint = new UnixDomainSocketEndPoint(socketPath);
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

                await socket.ConnectAsync(endpoint);
                return new NetworkStream(socket, ownsSocket: true);
               
            },
            // Disable pooling to ensure ConnectCallback is always used
            PooledConnectionLifetime = TimeSpan.Zero,
            // Force HTTP/1.1 to avoid HTTP/2 complications
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                ApplicationProtocols = new List<System.Net.Security.SslApplicationProtocol>
                {
                    System.Net.Security.SslApplicationProtocol.Http11
                }
            }
        };

        return new UnixSocketHttpClient(handler)
        {
            BaseAddress = new Uri("http://unix_socket")
        };
    }
}
