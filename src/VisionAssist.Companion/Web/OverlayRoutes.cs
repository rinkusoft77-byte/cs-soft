using System.Text.Json;

namespace VisionAssist.Companion;

/// <summary>
/// Everything the local server answers:
///
///   POST /gsi      the game's Game State Integration payload
///   GET  /state    the current snapshot, for a page that just loaded
///   GET  /events   SSE stream of snapshots
///   GET  /config   display defaults from companion.json
///   GET  /health   plain "ok", for checking the port by hand
///   GET  /...      the overlay page and its assets
/// </summary>
public sealed class OverlayRoutes
{
    private static readonly JsonSerializerOptions Wire = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };

    private static readonly JsonSerializerOptions Incoming = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly CompanionConfig _config;
    private readonly StateStore _store;
    private readonly EventStream _events;
    private readonly StaticContent _content;
    private readonly bool _verbose;

    private int _rejectedTokenCount;

    public OverlayRoutes(CompanionConfig config, StateStore store, EventStream events,
        StaticContent content, bool verbose)
    {
        _config = config;
        _store = store;
        _events = events;
        _content = content;
        _verbose = verbose;
    }

    /// <summary>Raised when a payload changes the snapshot, so Program can log it.</summary>
    public event Action<OverlaySnapshot>? SnapshotApplied;

    public async Task HandleAsync(HttpExchange exchange, CancellationToken token)
    {
        switch (exchange.Path)
        {
            case "/gsi":
                await HandleGsiAsync(exchange, token).ConfigureAwait(false);
                return;

            case "/state":
                await exchange.WriteJsonAsync(Serialize(_store.Current), token).ConfigureAwait(false);
                return;

            case "/config":
                await exchange.WriteJsonAsync(
                    JsonSerializer.Serialize(_config.ToClientView(), Wire), token).ConfigureAwait(false);
                return;

            case "/events":
                await _events.AttachAsync(exchange, Serialize(_store.Current), token).ConfigureAwait(false);
                return;

            case "/health":
                await exchange.WriteTextAsync(200, "ok", token).ConfigureAwait(false);
                return;

            default:
                await ServeFileAsync(exchange, token).ConfigureAwait(false);
                return;
        }
    }

    // -------------------------------------------------------------------- GSI

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
        catch (JsonException ex)
        {
            if (_verbose) Console.Error.WriteLine($"visionassist: unreadable payload: {ex.Message}");
            await exchange.WriteTextAsync(400, "bad json", token).ConfigureAwait(false);
            return;
        }

        if (payload is null)
        {
            await exchange.WriteTextAsync(400, "empty payload", token).ConfigureAwait(false);
            return;
        }

        if (_config.RequireToken && payload.Auth?.Token != _config.Token)
        {
            // Only complain once. A wrong token means the cfg and companion.json
            // disagree, and repeating it every 100 ms would bury the message.
            if (Interlocked.Increment(ref _rejectedTokenCount) == 1)
            {
                Console.Error.WriteLine(
                    "visionassist: rejected a payload - the token in " +
                    "gamestate_integration_visionassist.cfg does not match companion.json");
            }

            await exchange.WriteTextAsync(403, "bad token", token).ConfigureAwait(false);
            return;
        }

        var snapshot = _store.Apply(payload);
        _events.Broadcast("state", Serialize(snapshot));
        SnapshotApplied?.Invoke(snapshot);

        // The game does not read the body; it only wants a prompt 200 so the
        // next payload is not delayed by the timeout in the cfg.
        await exchange.WriteTextAsync(200, "ok", token).ConfigureAwait(false);
    }

    // ----------------------------------------------------------------- static

    private async Task ServeFileAsync(HttpExchange exchange, CancellationToken token)
    {
        if (exchange.Method != "GET")
        {
            await exchange.WriteTextAsync(405, "GET only", token).ConfigureAwait(false);
            return;
        }

        string? file = _content.Resolve(exchange.Path);
        if (file is null)
        {
            string message = _content.Exists
                ? $"not found: {exchange.Path}"
                : $"the overlay folder is missing: {_content.Root}";
            await exchange.WriteTextAsync(404, message, token).ConfigureAwait(false);
            return;
        }

        byte[] bytes = await File.ReadAllBytesAsync(file, token).ConfigureAwait(false);
        await exchange.WriteBytesAsync(200, StaticContent.ContentTypeFor(file), bytes, token)
            .ConfigureAwait(false);
    }

    public string Serialize(OverlaySnapshot snapshot) => JsonSerializer.Serialize(snapshot, Wire);
}
