namespace BaiduShareTool.Domain.Entities;

/// <summary>应用配置实体。</summary>
public sealed record AppSetting(string Key, string Value, DateTimeOffset UpdatedAt);
