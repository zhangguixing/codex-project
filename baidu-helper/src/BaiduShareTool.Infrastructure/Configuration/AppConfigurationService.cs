using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BaiduShareTool.Application.Configuration;
using Microsoft.Extensions.Logging;

namespace BaiduShareTool.Infrastructure.Configuration;

/// <summary>使用 Windows DPAPI 保护配置文件的实现。</summary>
public sealed class AppConfigurationService(ILogger<AppConfigurationService> logger) : IAppConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly SemaphoreSlim gate = new(1, 1);
    private string FilePath => Path.Combine(AppContext.BaseDirectory, "data", "configuration.dat");

    public async Task<AppConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(FilePath)) return new AppConfiguration();
            // 启动阶段可能由 ViewModel 同步读取配置；这里使用小型配置文件的同步读取，避免 UI 线程等待异步延续造成死锁。
            var encrypted = File.ReadAllBytes(FilePath);
            var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<AppConfiguration>(plain, JsonOptions) ?? new AppConfiguration();
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException or IOException)
        {
            logger.LogWarning(exception, "配置文件读取失败，将使用默认配置");
            return new AppConfiguration();
        }
        finally { gate.Release(); }
    }

    public async Task SaveAsync(AppConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(directory);
            var plain = JsonSerializer.SerializeToUtf8Bytes(configuration, JsonOptions);
            var encrypted = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
            var temporaryPath = FilePath + ".tmp";
            await File.WriteAllBytesAsync(temporaryPath, encrypted, cancellationToken);
            File.Move(temporaryPath, FilePath, true);
            logger.LogInformation("配置已保存");
        }
        finally { gate.Release(); }
    }
}
