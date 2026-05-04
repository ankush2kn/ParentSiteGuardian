namespace ParentSiteGuardian.Service.Matching;

/// <summary>
/// Matches HTTP(S) URLs against site rules (domain and pathPrefix). Invalid URLs yield no match; no exceptions.
/// </summary>
public static class UrlMatcher
{
    public const string PatternTypeDomain = "domain";
    public const string PatternTypePathPrefix = "pathPrefix";

    /// <summary>
    /// Returns true if <paramref name="url"/> matches the rule (<paramref name="pattern"/> + <paramref name="patternType"/>).
    /// </summary>
    public static bool IsMatch(string url, string pattern, string patternType)
    {
        if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(patternType))
            return false;

        if (!TryParseHttpUrl(url, out var uri))
            return false;

        var hostNorm = NormalizeHost(uri.Host);
        var pathNorm = NormalizePath(uri.AbsolutePath);
        var type = patternType.Trim();

        if (string.Equals(type, PatternTypeDomain, StringComparison.OrdinalIgnoreCase))
            return MatchesDomain(hostNorm, pattern);

        if (string.Equals(type, PatternTypePathPrefix, StringComparison.OrdinalIgnoreCase))
            return MatchesPathPrefix(hostNorm, pathNorm, pattern);

        return false;
    }

    private static bool TryParseHttpUrl(string url, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed))
            return false;

        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
            return false;

        if (string.IsNullOrEmpty(parsed.Host))
            return false;

        uri = parsed;
        return true;
    }

    /// <summary>Lowercase host and strip a single leading www. for comparison (implementation plan).</summary>
    private static string NormalizeHost(string host)
    {
        var h = host.ToLowerInvariant();
        if (h.StartsWith("www.", StringComparison.Ordinal))
            h = h[4..];
        return h;
    }

    private static string NormalizePath(string absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath))
            return "/";
        return absolutePath.ToLowerInvariant();
    }

    /// <summary>Pattern is a registrable domain; host matches if equal or is a subdomain (e.g. m.youtube.com).</summary>
    private static bool MatchesDomain(string hostNormalized, string pattern)
    {
        var p = NormalizeHost(pattern.Trim().ToLowerInvariant());
        if (string.IsNullOrEmpty(p))
            return false;

        return hostNormalized.Equals(p, StringComparison.Ordinal)
               || hostNormalized.EndsWith("." + p, StringComparison.Ordinal);
    }

    /// <summary>Pattern is host/pathPrefix, e.g. bing.com/videos — query string ignored via Uri parsing.</summary>
    private static bool MatchesPathPrefix(string hostNormalized, string pathNormalized, string pattern)
    {
        var trimmed = pattern.Trim();
        var slash = trimmed.IndexOf('/');
        if (slash <= 0 || slash >= trimmed.Length - 1)
            return false;

        var hostPart = NormalizeHost(trimmed[..slash].ToLowerInvariant());
        var pathPart = trimmed[(slash + 1)..].Trim().TrimStart('/');

        if (string.IsNullOrEmpty(hostPart) || string.IsNullOrEmpty(pathPart))
            return false;

        if (!hostNormalized.Equals(hostPart, StringComparison.Ordinal))
            return false;

        var prefix = "/" + pathPart.ToLowerInvariant();
        return pathNormalized.Equals(prefix, StringComparison.Ordinal)
               || pathNormalized.StartsWith(prefix + "/", StringComparison.Ordinal);
    }
}
