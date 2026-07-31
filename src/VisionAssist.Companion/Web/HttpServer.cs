using System.Net;
using System.Net.Sockets;

namespace VisionAssist.Companion;

/// <summary>
/// A minimal HTTP/1.1 server on a raw TcpListener.
///
/// HttpListener would be less code, but on Windows it goes through http.sys,
/// which needs an admin-registered URL reservation for an arbitrary port. A
/// plain socket needs no such thing, and this tool has to start by
/// double-clicking it.
/// </summary>
public sealed class HttpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<HttpExchange, CancellationToken, Task> _handler;
    private readonly CancellationTokenSource _cts = new();

    public HttpServer(int port, Func<HttpExchange, CancellationToken, Task> handler)
    {
        // Loopback only. The overlay is for the person at the keyboard; binding
        // any other interface would put it on the network.
        _listener = new TcpListener(IPAddress.Loopback, port);
        _handler = handler;
    }

    public void Start()
    {
        _listener.Start();
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token), CancellationToken.None);
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
            {
                return;
            }

            _ = Task.Run(() => ServeAsync(client, token), CancellationToken.None);
        }
    }

    private async Task ServeAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        {
            try
            {
                client.NoDelay = true;
                var stream = client.GetStream();

                var exchange = await HttpExchange.ReadAsync(stream, token).ConfigureAwait(false);
                if (exchange is null) return;

                await _handler(exchange, token).ConfigureAwait(false);

                if (!exchange.Responded)
                    await exchange.WriteTextAsync(404, "not found", token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException or SocketException)
            {
                // Browser navigated away or the game timed out the POST. Normal.
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"visionassist: request failed: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _listener.Stop();
        }
        catch (SocketException)
        {
            // Already down.
        }
        _cts.Dispose();
    }
}
