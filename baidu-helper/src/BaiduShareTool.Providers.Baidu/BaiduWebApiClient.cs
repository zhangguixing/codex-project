using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using BaiduShareTool.Application.Configuration;
using BaiduShareTool.Providers.Abstractions;
using Microsoft.Extensions.Logging;

namespace BaiduShareTool.Providers.Baidu;

/// <summary>基于用户网页登录会话的百度网盘网页接口兼容层。</summary>
public sealed class BaiduWebApiClient(IAppConfigurationService configurationService, IHttpClientFactory httpClientFactory, ILogger<BaiduWebApiClient> logger)
{
    public async Task<BaiduShareTransferResult> TransferSharedFilesAsync(
        BaiduShareTransferRequest request,
        IProgress<BaiduShareTransferProgress>? progress = null,
        Func<CancellationToken, Task>? waitIfPaused = null,
        CancellationToken cancellationToken = default)
    {
        const int apiBatchLimit = 500;
        var shortUrl = GetShortUrl(request.ShareUrl);
        var token = await GetBdstokenAsync(cancellationToken);
        var password = string.IsNullOrWhiteSpace(request.Password) ? GetSharePassword(request.ShareUrl) : request.Password;
        var verification = await VerifySharePasswordAsync(shortUrl, password, token, cancellationToken);
        verification = await LoadShareMetadataAsync(verification, cancellationToken);
        var files = await ListSharedFilesAsync(shortUrl, verification, cancellationToken);
        var batchSize = request.SplitIntoBatches ? apiBatchLimit : Math.Max(1, files.Count);
        var selectedFiles = files;
        var batchCount = selectedFiles.Count == 0 ? 0 : (int)Math.Ceiling(selectedFiles.Count / (double)batchSize);
        var transferred = 0;

        for (var batchIndex = 0; batchIndex < batchCount; batchIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (waitIfPaused is not null) await waitIfPaused(cancellationToken);
            var batch = selectedFiles.Skip(batchIndex * batchSize).Take(batchSize).ToArray();
            await TransferBatchAsync(verification, batch, request.DestinationPath, token, cancellationToken);
            transferred += batch.Length;
            progress?.Report(new BaiduShareTransferProgress(transferred, selectedFiles.Count, batchIndex + 1, batchCount));
            if (batchIndex + 1 < batchCount && request.BatchDelayMilliseconds > 0)
                await Task.Delay(request.BatchDelayMilliseconds, cancellationToken);
        }

        return new BaiduShareTransferResult(files.Count, transferred, batchCount);
    }

    public async Task<AuthResult> ValidateAsync(CancellationToken cancellationToken)
    {
        try { await GetBdstokenAsync(cancellationToken); return new AuthResult(true, "网页登录会话有效"); }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException) { return new AuthResult(false, exception.Message); }
    }

    public async Task<IReadOnlyList<BaiduRemoteEntry>> ListAsync(string path, CancellationToken cancellationToken)
    {
        var token = await GetBdstokenAsync(cancellationToken);
        var uri = $"https://pan.baidu.com/api/list?dir={Uri.EscapeDataString(path)}&order=name&desc=0&showempty=0&web=1&page=1&num=1000&bdstoken={Uri.EscapeDataString(token)}";
        using var response = await SendAsync(HttpMethod.Get, uri, null, cancellationToken);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        EnsureSuccess(json.RootElement);
        if (!json.RootElement.TryGetProperty("list", out var list)) return [];
        return list.EnumerateArray().Select(item => new BaiduRemoteEntry(item.GetProperty("fs_id").GetInt64().ToString(), item.GetProperty("path").GetString() ?? path, item.GetProperty("server_filename").GetString() ?? string.Empty, item.TryGetProperty("isdir", out var isdir) && isdir.GetInt32() == 1, item.TryGetProperty("size", out var size) ? size.GetInt64() : 0)).ToArray();
    }

    public async Task<ShareResult> CreateShareAsync(RemoteFile file, ShareOptions options, CancellationToken cancellationToken)
    {
        var password = options.Password ?? string.Empty;
        if (password.Length != 4 || !password.All(char.IsLetterOrDigit))
            return new ShareResult(false, ErrorMessage: "百度网盘提取码必须为 4 位英文字母或数字");
        var random = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var form = new Dictionary<string, string>
        {
            ["path_list"] = JsonSerializer.Serialize(new[] { file.Path }),
            ["channel_list"] = "[]",
            ["period"] = options.ValidDays.ToString(),
            ["schannel"] = "4",
            ["is_card"] = "0",
            ["pwd"] = password
        };
        var shareSetUri = $"https://pan.baidu.com/act/public/agentwebmastertools/createsharewithpwd?channel=chunlei&clienttype=25&web=1&time={timestamp}&rand={random}&version=2.2.0";
        using var response = await SendAsync(HttpMethod.Post, shareSetUri, new FormUrlEncodedContent(form), cancellationToken, "https://pan.baidu.com/act/public/agentwebmastertools/index");
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("百度带密码分享接口响应：{Response}", responseText.Replace(password, "****", StringComparison.Ordinal));
        using var json = JsonDocument.Parse(responseText);
        EnsureSuccess(json.RootElement);
        var link = FindLink(json.RootElement);
        if (!string.IsNullOrWhiteSpace(link))
        {
            // 百度短链接默认不显示提取码，导出时显式附加 pwd 方便复制使用。
            var separator = link.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            return new ShareResult(true, $"{link}{separator}pwd={Uri.EscapeDataString(password)}");
        }
        return new ShareResult(false, ErrorMessage: "分享接口未返回链接");
    }

    private static string? FindLink(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in new[] { "link", "sharelink", "share_link", "shareUrl", "share_url", "url" })
                if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && value.GetString()?.Contains("pan.baidu.com", StringComparison.OrdinalIgnoreCase) == true) return value.GetString();
            foreach (var property in element.EnumerateObject()) { var nested = FindLink(property.Value); if (!string.IsNullOrWhiteSpace(nested)) return nested; }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) { var nested = FindLink(item); if (!string.IsNullOrWhiteSpace(nested)) return nested; }
        return null;
    }

    private async Task<BaiduShareVerification> VerifySharePasswordAsync(string shortUrl, string password, string token, CancellationToken cancellationToken)
    {
        var uri = $"https://pan.baidu.com/share/verify?surl={Uri.EscapeDataString(shortUrl)}&bdstoken={Uri.EscapeDataString(token)}&channel=chunlei&clienttype=0&web=1";
        using var response = await SendAsync(HttpMethod.Post, uri, new FormUrlEncodedContent(new Dictionary<string, string> { ["pwd"] = password.Trim() }), cancellationToken, "https://pan.baidu.com/share/init");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        EnsureSuccess(json.RootElement);
        var root = json.RootElement;
        var data = root.TryGetProperty("data", out var value) ? value : root;
        var randSk = data.TryGetProperty("randsk", out var randsk) ? randsk.GetString() : null;
        return new BaiduShareVerification(string.Empty, string.Empty, randSk, shortUrl);
    }

    private async Task<BaiduShareVerification> LoadShareMetadataAsync(BaiduShareVerification verification, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, $"https://pan.baidu.com/s/1{Uri.EscapeDataString(verification.ShortUrl)}", null, cancellationToken, "https://pan.baidu.com/share/init");
        var page = await response.Content.ReadAsStringAsync(cancellationToken);
        var shareId = GetSharePageNumber(page, "SHARE_ID", "shareid");
        var uk = GetSharePageNumber(page, "SHARE_UK", "share_uk", "shareUk", "uk");
        return verification with { ShareId = shareId, Uk = uk };
    }

    private async Task<IReadOnlyList<long>> ListSharedFilesAsync(string shortUrl, BaiduShareVerification verification, CancellationToken cancellationToken)
    {
        var ids = new List<long>();
        var page = 1;
        while (true)
        {
            var sekey = string.IsNullOrWhiteSpace(verification.RandSk) ? string.Empty : $"&sekey={Uri.EscapeDataString(verification.RandSk)}";
            var uri = $"https://pan.baidu.com/share/list?shorturl={Uri.EscapeDataString(shortUrl)}&shareid={Uri.EscapeDataString(verification.ShareId)}&uk={Uri.EscapeDataString(verification.Uk)}&root=1&page={page}&num=100&order=name&desc=0&showempty=0&channel=chunlei&clienttype=12&web=1{sekey}";
            using var response = await SendAsync(HttpMethod.Get, uri, null, cancellationToken, "https://pan.baidu.com/share/init");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            EnsureSuccess(json.RootElement);
            if (!json.RootElement.TryGetProperty("list", out var list) || list.GetArrayLength() == 0) break;
            ids.AddRange(list.EnumerateArray().Where(item => item.TryGetProperty("fs_id", out _)).Select(item => item.GetProperty("fs_id").GetInt64()));
            var hasMore = json.RootElement.TryGetProperty("has_more", out var hasMoreValue) && hasMoreValue.GetInt32() == 1;
            if (!hasMore) break;
            page++;
        }
        return ids;
    }

    private async Task TransferBatchAsync(BaiduShareVerification verification, IReadOnlyList<long> ids, string destinationPath, string token, CancellationToken cancellationToken)
    {
        var sekey = string.IsNullOrWhiteSpace(verification.RandSk) ? string.Empty : $"&sekey={Uri.EscapeDataString(verification.RandSk)}";
        var uri = $"https://pan.baidu.com/share/transfer?shareid={Uri.EscapeDataString(verification.ShareId)}&from={Uri.EscapeDataString(verification.Uk)}{sekey}&ondup=newcopy&async=1&channel=chunlei&web=1&app_id=250528&bdstoken={Uri.EscapeDataString(token)}&clienttype=0";
        var form = new Dictionary<string, string>
        {
            ["fsidlist"] = JsonSerializer.Serialize(ids),
            ["path"] = destinationPath
        };
        using var response = await SendAsync(HttpMethod.Post, uri, new FormUrlEncodedContent(form), cancellationToken, $"https://pan.baidu.com/s/1{verification.ShortUrl}");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        EnsureSuccess(json.RootElement);
    }

    private static string GetShortUrl(string shareUrl)
    {
        if (!Uri.TryCreate(shareUrl.Trim(), UriKind.Absolute, out var uri)) throw new InvalidOperationException("分享链接格式不正确");
        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var marker = Array.FindIndex(parts, item => string.Equals(item, "s", StringComparison.OrdinalIgnoreCase));
        var value = marker >= 0 && marker + 1 < parts.Length ? parts[marker + 1] : parts.LastOrDefault();
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("无法从分享链接中识别分享码");
        return value.StartsWith("1", StringComparison.Ordinal) ? value[1..] : value;
    }

    private static string GetSharePassword(string shareUrl)
    {
        if (!Uri.TryCreate(shareUrl.Trim(), UriKind.Absolute, out var uri)) return string.Empty;
        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in query)
        {
            var pair = item.Split('=', 2);
            if (pair.Length == 2 && string.Equals(Uri.UnescapeDataString(pair[0]), "pwd", StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(pair[1]).Trim();
        }
        return string.Empty;
    }

    private static string GetSharePageNumber(string page, params string[] names)
    {
        foreach (var name in names)
        {
            var pattern = $@"(?:[""']?{Regex.Escape(name)}[""']?\s*[:=]\s*|setData\(\s*[""']{Regex.Escape(name)}[""']\s*,\s*)[""']?([0-9]+)[""']?";
            var match = Regex.Match(page, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success) return match.Groups[1].Value;
        }
        throw new InvalidOperationException($"无法从分享页读取 {names[0]}");
    }

    private async Task<string> GetBdstokenAsync(CancellationToken cancellationToken)
    {
        using var response = await SendAsync(HttpMethod.Get, "https://pan.baidu.com/api/gettemplatevariable?fields=%5B%22bdstoken%22%5D", null, cancellationToken);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var root = json.RootElement;
        if (root.TryGetProperty("result", out var result) && result.TryGetProperty("bdstoken", out var token) && !string.IsNullOrWhiteSpace(token.GetString())) return token.GetString()!;
        throw new InvalidOperationException("未获取到 bdstoken，请重新登录百度网盘");
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, HttpContent? content, CancellationToken cancellationToken, string? referer = null)
    {
        var configuration = await configurationService.LoadAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(configuration.Baidu.Cookie)) throw new InvalidOperationException("请先完成百度网盘网页登录");
        var request = new HttpRequestMessage(method, uri) { Content = content };
        request.Headers.TryAddWithoutValidation("Cookie", configuration.Baidu.Cookie);
        request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
        request.Headers.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
        request.Headers.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.9");
        request.Headers.Referrer = new Uri(referer ?? "https://pan.baidu.com/disk/main");
        request.Headers.TryAddWithoutValidation("Origin", "https://pan.baidu.com");
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        return await httpClientFactory.CreateClient("baidu-web").SendAsync(request, cancellationToken);
    }

    private static void EnsureSuccess(JsonElement root)
    {
        if (!root.TryGetProperty("errno", out var errno) || errno.GetInt32() == 0) return;
        var code = errno.GetInt32();
        var message = root.TryGetProperty("errmsg", out var messageValue) ? messageValue.GetString() : string.Empty;
        if (code == 9019)
            throw new BaiduAccountException("百度网盘返回 9019（账号异常/安全风控），已停止转存。请检查当前登录账号和分享来源的状态，稍后重试或重新登录。", code);
        throw new InvalidOperationException($"百度接口错误：{code} {message}");
    }
}

public sealed record BaiduRemoteEntry(string Id, string Path, string Name, bool IsDirectory, long Size);
public sealed record BaiduShareTransferRequest(string ShareUrl, string? Password, string DestinationPath, bool SplitIntoBatches, int BatchSize = 500, int BatchDelayMilliseconds = 1200);
public sealed record BaiduShareTransferProgress(int TransferredCount, int TotalCount, int CurrentBatch, int TotalBatches);
public sealed record BaiduShareTransferResult(int AvailableCount, int TransferredCount, int BatchCount);
internal sealed record BaiduShareVerification(string ShareId, string Uk, string? RandSk, string ShortUrl);
public sealed class BaiduAccountException(string message, int errorCode) : InvalidOperationException(message)
{
    public int ErrorCode { get; } = errorCode;
}
