using System.Buffers;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReignsAccess.Installer;

internal sealed record ReleaseAsset(string Name, Uri DownloadUrl, long Size, string? Digest);

internal sealed record ModRelease(
    string Tag,
    string Name,
    bool IsPrerelease,
    DateTimeOffset? PublishedAt,
    ReleaseAsset Asset)
{
    public override string ToString() => IsPrerelease ? $"{Tag} (test release)" : Tag;
}

internal sealed class GitHubReleaseClient : IDisposable
{
    internal const string Repository = "leoguimaoficial/Reigns-Access";
    private readonly HttpClient _httpClient;

    public GitHubReleaseClient()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ReignsAccessInstaller");
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<IReadOnlyList<ModRelease>> GetReleasesAsync(CancellationToken cancellationToken)
    {
        var uri = new Uri($"https://api.github.com/repos/{Repository}/releases?per_page=100");
        await using var stream = await _httpClient.GetStreamAsync(uri, cancellationToken);
        var releases = await JsonSerializer.DeserializeAsync<List<GitHubReleaseDto>>(
            stream,
            cancellationToken: cancellationToken) ?? [];

        return releases
            .Where(release => !release.Draft)
            .Select(release => (release, asset: SelectPackageAsset(release.Assets)))
            .Where(pair => pair.asset is not null)
            .Select(pair => new ModRelease(
                pair.release.TagName,
                string.IsNullOrWhiteSpace(pair.release.Name) ? pair.release.TagName : pair.release.Name,
                pair.release.Prerelease,
                pair.release.PublishedAt,
                new ReleaseAsset(
                    pair.asset!.Name,
                    new Uri(pair.asset.BrowserDownloadUrl),
                    pair.asset.Size,
                    pair.asset.Digest)))
            .ToArray();
    }

    public async Task DownloadAsync(
        ReleaseAsset asset,
        string destination,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            asset.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? asset.Size;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(
            destination,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);

        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            long downloaded = 0;
            while (true)
            {
                var read = await input.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                    break;

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                downloaded += read;
                if (totalBytes > 0)
                    progress?.Report((int)Math.Clamp(downloaded * 100 / totalBytes, 0, 100));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    internal static GitHubAssetDto? SelectPackageAsset(IEnumerable<GitHubAssetDto> assets)
    {
        var zipAssets = assets
            .Where(asset => asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            .Where(asset => !asset.Name.Contains("installer", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var bundle = zipAssets.FirstOrDefault(asset =>
            asset.Name.Contains("bepinex", StringComparison.OrdinalIgnoreCase) ||
            asset.Name.Contains("bundle", StringComparison.OrdinalIgnoreCase) ||
            asset.Name.Contains("full", StringComparison.OrdinalIgnoreCase));

        return bundle ?? (zipAssets.Length == 1 ? zipAssets[0] : null);
    }

    public void Dispose() => _httpClient.Dispose();

    internal sealed class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = "";

        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("draft")]
        public bool Draft { get; init; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; init; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset? PublishedAt { get; init; }

        [JsonPropertyName("assets")]
        public List<GitHubAssetDto> Assets { get; init; } = [];
    }

    internal sealed class GitHubAssetDto
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; init; } = "";

        [JsonPropertyName("size")]
        public long Size { get; init; }

        [JsonPropertyName("digest")]
        public string? Digest { get; init; }
    }
}
