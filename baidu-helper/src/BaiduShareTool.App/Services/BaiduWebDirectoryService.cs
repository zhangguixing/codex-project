using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using BaiduShareTool.Application.Configuration;
namespace BaiduShareTool.App.Services;
public sealed class BaiduWebDirectoryService(IAppConfigurationService configurationService)
{
    public async Task<IReadOnlyList<BaiduDirectoryEntry>> ListAsync(string path, CancellationToken cancellationToken = default)
    {
        var configuration = await configurationService.LoadAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(configuration.Baidu.Cookie)) throw new InvalidOperationException("请先完成网页登录");
        var uri = $"https://pan.baidu.com/api/list?dir={Uri.EscapeDataString(path)}&order=name&desc=0&showempty=0&web=1&page=1&num=1000";
        using var client = new HttpClient(); using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.TryAddWithoutValidation("Cookie", configuration.Baidu.Cookie); request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
        using var response = await client.SendAsync(request, cancellationToken); response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken)); var root = document.RootElement;
        if (root.TryGetProperty("errno", out var errno) && errno.GetInt32() != 0) throw new InvalidOperationException($"目录读取失败，错误码：{errno.GetInt32()}");
        if (!root.TryGetProperty("list", out var list)) return [];
        return list.EnumerateArray().Where(item => item.TryGetProperty("isdir", out var isdir) && isdir.GetInt32() == 1).Select(item => new BaiduDirectoryEntry(item.GetProperty("path").GetString() ?? "/", item.GetProperty("server_filename").GetString() ?? "未命名目录")).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
public sealed record BaiduDirectoryEntry(string Path, string Name);
