namespace VisionAssist.Companion;

/// <summary>
/// Serves the overlay page from a folder on disk, so the HTML and CSS can be
/// tweaked and reloaded without rebuilding.
/// </summary>
public sealed class StaticContent
{
    private readonly string _root;

    public StaticContent(string root) => _root = Path.GetFullPath(root);

    public string Root => _root;

    public bool Exists => Directory.Exists(_root);

    /// <summary>
    /// Maps a request path to a file inside the root, or null when the file is
    /// missing or the path tried to climb out of it.
    /// </summary>
    public string? Resolve(string requestPath)
    {
        string relative = requestPath.TrimStart('/');
        if (relative.Length == 0) relative = "index.html";

        // Reject before touching the filesystem: '..' is the whole attack, and
        // a rooted path would make Path.Combine ignore the root entirely.
        if (relative.Contains("..", StringComparison.Ordinal)) return null;
        if (Path.IsPathRooted(relative)) return null;

        string candidate = Path.GetFullPath(Path.Combine(_root, relative));

        // Belt and braces: symlinks and odd encodings could still land outside.
        if (!candidate.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && candidate != _root)
        {
            return null;
        }

        return File.Exists(candidate) ? candidate : null;
    }

    public static string ContentTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".js" => "text/javascript; charset=utf-8",
        ".json" => "application/json; charset=utf-8",
        ".svg" => "image/svg+xml",
        ".png" => "image/png",
        ".woff2" => "font/woff2",
        ".ico" => "image/x-icon",
        _ => "application/octet-stream",
    };
}
