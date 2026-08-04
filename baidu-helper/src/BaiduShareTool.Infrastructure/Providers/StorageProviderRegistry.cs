using BaiduShareTool.Providers.Abstractions;

namespace BaiduShareTool.Infrastructure.Providers;

/// <summary>默认 Provider 注册表实现。</summary>
public sealed class StorageProviderRegistry(IEnumerable<IStorageProvider> providers) : IStorageProviderRegistry
{
    public IReadOnlyCollection<IStorageProvider> Providers { get; } = providers.ToArray();

    public IStorageProvider GetRequired(string providerId)
        => Providers.FirstOrDefault(item => string.Equals(item.ProviderId, providerId, StringComparison.OrdinalIgnoreCase))
           ?? throw new KeyNotFoundException($"未注册 Provider：{providerId}");
}
