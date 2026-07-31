using System.Text;

namespace VisionAssist.Companion;

/// <summary>
/// One request/response pair on an already-accepted socket. Deliberately small:
/// the overlay needs GET, POST with a Content-Length body, and one long-lived
/// text/event-stream response, and nothing else.
/// </summary>
public sealed class HttpExchange
{
    private const int MaxHeaderBytes = 32 * 1024;
    private const int MaxBodyBytes = 4 * 1024 * 1024;

    private readonly Stream _stream;

    private HttpExchange(Stream stream, string method, string path, string query,
        Dictionary<string, string> headers, byte[] body)
    {
        _stream = stream;
        Method = method;
        Path = path;
        Query = query;
        Headers = headers;
        Body = body;
    }

    public string Method { get; }

    /// <summary>Percent-decoded path, always starting with a slash.</summary>
    public string Path { get; }

    /// <summary>Raw query string without the leading '?'.</summary>
    public string Query { get; }

    public Dictionary<string, string> Headers { get; }

    public byte[] Body { get; }

    /// <summary>Set once a response has been written, so nothing writes twice.</summary>
    public bool Responded { get; private set; }

    public string BodyText => Encoding.UTF8.GetString(Body);

    // ------------------------------------------------------------------ read

    /// <summary>
    /// Reads one request. Returns null when the peer closed the connection or
    /// sent something malformed - in both cases the caller just drops the socket.
    /// </summary>
    public static async Task<HttpExchange?> ReadAsync(Stream stream, CancellationToken token)
    {
        var buffer = new byte[8192];
        using var accumulated = new MemoryStream();
        int headerEnd = -1;

        while (headerEnd < 0)
        {
            int read = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
            if (read <= 0) return null;

            accumulated.Write(buffer, 0, read);
            if (accumulated.Length > MaxHeaderBytes) return null;

            headerEnd = FindHeaderEnd(accumulated.GetBuffer(), (int)accumulated.Length);
        }

        byte[] raw = accumulated.GetBuffer();
        string headerText = Encoding.UTF8.GetString(raw, 0, headerEnd);
        string[] lines = headerText.Split("\r\n");
        if (lines.Length == 0) return null;

        string[] requestLine = lines[0].Split(' ');
        if (requestLine.Length < 2) return null;

        string method = requestLine[0].ToUpperInvariant();
        string target = requestLine[1];

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < lines.Length; i++)
        {
            int colon = lines[i].IndexOf(':');
            if (colon <= 0) continue;
            headers[lines[i][..colon].Trim()] = lines[i][(colon + 1)..].Trim();
        }

        // Whatever followed the blank line is the first slice of the body.
        int bodyStart = headerEnd + 4;
        int alreadyRead = (int)accumulated.Length - bodyStart;

        int contentLength = 0;
        if (headers.TryGetValue("Content-Length", out string? lengthHeader)
            && int.TryParse(lengthHeader, out int parsedLength))
        {
            if (parsedLength is < 0 or > MaxBodyBytes) return null;
            contentLength = parsedLength;
        }

        var body = new byte[contentLength];
        if (contentLength > 0)
        {
            int copied = Math.Min(alreadyRead, contentLength);
            Array.Copy(raw, bodyStart, body, 0, copied);

            while (copied < contentLength)
            {
                int read = await stream.ReadAsync(body.AsMemory(copied), token).ConfigureAwait(false);
                if (read <= 0) return null;
                copied += read;
            }
        }

        int questionMark = target.IndexOf('?');
        string path = questionMark < 0 ? target : target[..questionMark];
        string query = questionMark < 0 ? string.Empty : target[(questionMark + 1)..];

        try
        {
            path = Uri.UnescapeDataString(path);
        }
        catch (UriFormatException)
        {
            return null;
        }

        if (!path.StartsWith('/')) path = "/" + path;

        return new HttpExchange(stream, method, path, query, headers, body);
    }

    private static int FindHeaderEnd(byte[] buffer, int length)
    {
        for (int i = 0; i + 3 < length; i++)
        {
            if (buffer[i] == '\r' && buffer[i + 1] == '\n' && buffer[i + 2] == '\r' && buffer[i + 3] == '\n')
                return i;
        }
        return -1;
    }

    // ----------------------------------------------------------------- write

    public Task WriteJsonAsync(string json, CancellationToken token = default)
        => WriteBytesAsync(200, "application/json; charset=utf-8", Encoding.UTF8.GetBytes(json), token);

    public Task WriteTextAsync(int status, string text, CancellationToken token = default)
        => WriteBytesAsync(status, "text/plain; charset=utf-8", Encoding.UTF8.GetBytes(text), token);

    public async Task WriteBytesAsync(int status, string contentType, byte[] body,
        CancellationToken token = default)
    {
        if (Responded) return;
        Responded = true;

        var header = new StringBuilder();
        header.Append("HTTP/1.1 ").Append(status).Append(' ').Append(Reason(status)).Append("\r\n");
        header.Append("Content-Type: ").Append(contentType).Append("\r\n");
        header.Append("Content-Length: ").Append(body.Length).Append("\r\n");
        header.Append("Cache-Control: no-store\r\n");
        // The overlay is loopback-only; a page from anywhere else has no reason
        // to read it, so no CORS header is sent.
        header.Append("Connection: close\r\n\r\n");

        await _stream.WriteAsync(Encoding.ASCII.GetBytes(header.ToString()), token).ConfigureAwait(false);
        if (body.Length > 0) await _stream.WriteAsync(body, token).ConfigureAwait(false);
        await _stream.FlushAsync(token).ConfigureAwait(false);
    }

    /// <summary>
    /// Switches the connection into an SSE stream and hands back the raw stream.
    /// The caller owns it from here and keeps it open until the client leaves.
    /// </summary>
    public async Task<Stream> BeginEventStreamAsync(CancellationToken token = default)
    {
        Responded = true;

        const string header =
            "HTTP/1.1 200 OK\r\n" +
            "Content-Type: text/event-stream; charset=utf-8\r\n" +
            "Cache-Control: no-store\r\n" +
            "Connection: keep-alive\r\n" +
            "X-Accel-Buffering: no\r\n\r\n";

        await _stream.WriteAsync(Encoding.ASCII.GetBytes(header), token).ConfigureAwait(false);
        await _stream.FlushAsync(token).ConfigureAwait(false);
        return _stream;
    }

    private static string Reason(int status) => status switch
    {
        200 => "OK",
        204 => "No Content",
        400 => "Bad Request",
        403 => "Forbidden",
        404 => "Not Found",
        405 => "Method Not Allowed",
        500 => "Internal Server Error",
        _ => "OK",
    };
}
