namespace BaiduShareTool.Providers.Abstractions;

/// <summary>Provider 注册表，供应用层按标识选择网盘实现。</summary>
public interface IStorageProviderRegistry
{
    IReadOnlyCollection<IStorageProvider> Providers { get; }
    IStorageProvider GetRequired(string providerId);
}
