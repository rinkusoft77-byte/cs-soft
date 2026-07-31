using System.Text.Json;
using VisionAssist.Companion;

namespace VisionAssist.Overlay;

/// <summary>
/// Listens for the game's Game State Integration payloads.
///
/// The HTTP server, the payload model and the state store come from the
/// Companion project rather than being written a second time - this program is a
/// different face on the same data, not a different program.
/// </summary>
internal sealed class GsiHost : IDisposable
{
    private static readonly JsonSerializerOptions Incoming = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private static readonly JsonSerializerOptions Outgoing = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly StateStore _store = new();
    private readonly HttpServer _server;
    private readonly string _token;

    private long _lastUpdateTicks;
    private int _rejectedTokens;

    public GsiHost(int port, string token)
    {
        _token = token;
        _server = new HttpServer(port, HandleAsync);
    }

    /// <summary>Raised off the UI thread whenever a payload lands.</summary>
    public event Action? Updated;

    public OverlaySnapshot Current => _store.Current;

    public bool IsFresh => _store.IsFresh;

    /// <summary>A wrong token in the cfg, which is otherwise invisible.</summary>
    public bool HadTokenMismatch => Volatile.Read(ref _rejectedTokens) > 0;

    /// <summary>Milliseconds since the last payload, for smoothing the countdown.</summary>
    public double MillisecondsSinceUpdate
    {
        get
        {
            long ticks = Interlocked.Read(ref _lastUpdateTicks);
            return ticks == 0 ? 0 : Environment.TickCount64 - ticks;
        }
    }

    /// <summary>Throws <see cref="System.Net.Sockets.SocketException"/> when the port is taken.</summary>
    public void Start() => _server.Start();

    /// <summary>
    /// Marks the state disconnected when the heartbeat stops. Called from the
    /// form's timer rather than a thread of its own.
    /// </summary>
    public bool CheckStale()
    {
        if (!_store.Current.Connected || _store.IsFresh) return false;
        _store.MarkDisconnected();
        return true;
    }

    private async Task HandleAsync(HttpExchange exchange, CancellationToken token)
    {
        switch (exchange.Path)
        {
            case "/gsi":
                await HandleGsiAsync(exchange, token).ConfigureAwait(false);
                return;

            // Kept so the browser overlay and curl still work against this
            // program, for anyone who prefers that window.
            case "/state":
                await exchange.WriteJsonAsync(
                    JsonSerializer.Serialize(_store.Current, Outgoing), token).ConfigureAwait(false);
                return;

            case "/health":
                await exchange.WriteTextAsync(200, "ok", token).ConfigureAwait(false);
                return;

            default:
                await exchange.WriteTextAsync(404, "not found", token).ConfigureAwait(false);
                return;
        }
    }

    private async Task HandleGsiAsync(HttpExchange exchange, CancellationToken token)
    {
        if (exchange.Method != "POST")
        {
            await exchange.WriteTextAsync(405, "POST only", token).ConfigureAwait(false);
            return;
        }

        GsiPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<GsiPayload>(exchange.BodyText, Incoming);
        }
        catch (JsonException)
        {
            await exchange.WriteTextAsync(400, "bad json", token).ConfigureAwait(false);
            return;
        }

        if (payload is null)
        {
            await exchange.WriteTextAsync(400, "empty payload", token).ConfigureAwait(false);
            return;
        }

        if (payload.Auth?.Token != _token)
        {
            Interlocked.Increment(ref _rejectedTokens);
            await exchange.WriteTextAsync(403, "bad token", token).ConfigureAwait(false);
            return;
        }

        _store.Apply(payload);
        Interlocked.Exchange(ref _lastUpdateTicks, Environment.TickCount64);
        Updated?.Invoke();

        // The game ignores the body and only wants a prompt 200, so the next
        // payload is not held up by the timeout in the cfg.
        await exchange.WriteTextAsync(200, "ok", token).ConfigureAwait(false);
    }

    public void Dispose() => _server.Dispose();
}
