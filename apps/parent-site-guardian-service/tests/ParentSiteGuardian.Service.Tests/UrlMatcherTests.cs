using ParentSiteGuardian.Service.Matching;

namespace ParentSiteGuardian.Service.Tests;

public class UrlMatcherTests
{
    [Theory]
    [InlineData("https://youtube.com/")]
    [InlineData("http://youtube.com/watch?v=1")]
    [InlineData("https://www.youtube.com/")]
    [InlineData("https://WWW.YOUTUBE.COM/watch?v=1")]
    [InlineData("https://m.youtube.com/")]
    [InlineData("https://www.m.youtube.com/feed/trending")]
    public void Domain_youtube_com_matches_subdomains_and_www(string url) =>
        Assert.True(UrlMatcher.IsMatch(url, "youtube.com", UrlMatcher.PatternTypeDomain));

    [Theory]
    [InlineData("https://youtu.be/abc123")]
    [InlineData("http://www.youtu.be/abc123")]
    [InlineData("https://m.youtu.be/x")]
    public void Domain_youtu_be_matches(string url) =>
        Assert.True(UrlMatcher.IsMatch(url, "youtu.be", UrlMatcher.PatternTypeDomain));

    [Theory]
    [InlineData("https://youtube-nocookie.com/embed/xyz")]
    [InlineData("https://www.youtube-nocookie.com/")]
    public void Domain_youtube_nocookie_com_matches(string url) =>
        Assert.True(UrlMatcher.IsMatch(url, "youtube-nocookie.com", UrlMatcher.PatternTypeDomain));

    [Theory]
    [InlineData("https://www.bing.com/videos")]
    [InlineData("https://bing.com/videos")]
    [InlineData("https://www.bing.com/videos/")]
    [InlineData("https://www.bing.com/videos/search?q=cats")]
    [InlineData("http://Bing.COM/videos/browse/news")]
    [InlineData("https://www.bing.com/videos/extra/deep/path")]
    public void PathPrefix_bing_videos_matches_with_www_deeper_paths_and_query(string url) =>
        Assert.True(UrlMatcher.IsMatch(url, "bing.com/videos", UrlMatcher.PatternTypePathPrefix));

    [Theory]
    [InlineData("https://www.bing.com/search?q=test")]
    [InlineData("https://bing.com/search")]
    [InlineData("https://www.bing.com/")]
    [InlineData("https://www.bing.com/videosx")] // not a path segment boundary
    [InlineData("https://www.bing.com/video")] // singular
    public void PathPrefix_bing_videos_does_not_match_search_or_other_paths(string url) =>
        Assert.False(UrlMatcher.IsMatch(url, "bing.com/videos", UrlMatcher.PatternTypePathPrefix));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("/relative/path")]
    [InlineData("//missing-scheme.com")]
    [InlineData("ftp://example.com/videos")]
    [InlineData("https://")]
    [InlineData("https:///nopath")]
    public void Invalid_or_non_http_urls_never_match(string url)
    {
        Assert.False(UrlMatcher.IsMatch(url, "youtube.com", UrlMatcher.PatternTypeDomain));
        Assert.False(UrlMatcher.IsMatch(url, "bing.com/videos", UrlMatcher.PatternTypePathPrefix));
    }

    [Fact]
    public void Invalid_urls_do_not_throw()
    {
        var ex = Record.Exception(() =>
        {
            UrlMatcher.IsMatch("%%%", "youtube.com", UrlMatcher.PatternTypeDomain);
            UrlMatcher.IsMatch("https://example.com", "", UrlMatcher.PatternTypeDomain);
            UrlMatcher.IsMatch("https://example.com", "example.com", "unknownType");
        });
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("https://notyoutube.com/")]
    [InlineData("https://evil.youtube.com.evil.com/")]
    [InlineData("https://youtube.computer/")]
    public void Domain_youtube_com_does_not_match_lookalike_hosts(string url) =>
        Assert.False(UrlMatcher.IsMatch(url, "youtube.com", UrlMatcher.PatternTypeDomain));

    [Fact]
    public void PathPrefix_accepts_www_in_stored_pattern_host()
    {
        Assert.True(UrlMatcher.IsMatch(
            "https://bing.com/videos",
            "www.bing.com/videos",
            UrlMatcher.PatternTypePathPrefix));
    }

    [Fact]
    public void Domain_pattern_type_is_case_insensitive()
    {
        Assert.True(UrlMatcher.IsMatch("https://youtube.com/", "youtube.com", "DOMAIN"));
    }

    [Fact]
    public void PathPrefix_type_is_case_insensitive()
    {
        Assert.True(UrlMatcher.IsMatch("https://bing.com/videos", "bing.com/videos", "PATHPREFIX"));
    }
}
