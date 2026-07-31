using System.Text;

namespace VisionAssist.Companion;

/// <summary>
/// Server-sent events: one open connection per overlay window, each getting a
/// copy of every state update. SSE rather than a WebSocket because the browser
/// side is then four lines and reconnects on its own.
/// </summary>
public sealed class EventStream
{
    private readonly List<Subscriber> _subscribers = new();
    private readonly object _gate = new();

    public int SubscriberCount
    {
        get { lock (_gate) return _subscribers.Count; }
    }

    /// <summary>
    /// Holds the request open until the client disconnects. The periodic comment
    /// is both a keep-alive and how a vanished client gets noticed on an
    /// otherwise idle stream.
    /// </summary>
    public async Task AttachAsync(HttpExchange exchange, string initialPayload, CancellationToken token)
    {
        var stream = await exchange.BeginEventStreamAsync(token).ConfigureAwait(false);
        var subscriber = new Subscriber(stream);

        lock (_gate) _subscribers.Add(subscriber);

        try
        {
            subscriber.Send("state", initialPayload);

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(15), token).ConfigureAwait(false);
                subscriber.Ping();
            }
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException)
        {
            // The window was closed or refreshed.
        }
        finally
        {
            lock (_gate) _subscribers.Remove(subscriber);
        }
    }

    /// <summary>
    /// Pushes an event to every open window, dropping the ones that have gone.
    /// Writes are synchronous: the payloads are a couple of kilobytes going to
    /// a loopback socket, so there is nothing to gain from awaiting them.
    /// </summary>
    public void Broadcast(string eventName, string payload)
    {
        Subscriber[] targets;
        lock (_gate) targets = _subscribers.ToArray();

        foreach (var subscriber in targets)
        {
            if (subscriber.Send(eventName, payload)) continue;
            lock (_gate) _subscribers.Remove(subscriber);
        }
    }

    private sealed class Subscriber
    {
        private readonly Stream _stream;
        private readonly object _writeGate = new();

        public Subscriber(Stream stream) => _stream = stream;

        /// <summary>Returns false when the connection is gone.</summary>
        public bool Send(string eventName, string payload)
            => Write($"event: {eventName}\ndata: {payload}\n\n");

        /// <summary>A comment line: valid SSE, ignored by the browser.</summary>
        public void Ping()
        {
            if (!Write(":ping\n\n")) throw new IOException("subscriber gone");
        }

        private bool Write(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            try
            {
                lock (_writeGate)
                {
                    _stream.Write(bytes, 0, bytes.Length);
                    _stream.Flush();
                }
                return true;
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                return false;
            }
        }
    }
}
