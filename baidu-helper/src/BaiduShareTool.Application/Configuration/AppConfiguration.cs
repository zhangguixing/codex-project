namespace BaiduShareTool.Application.Configuration;

/// <summary>应用全部可持久化配置。</summary>
public sealed class AppConfiguration
{
    public BaiduConfiguration Baidu { get; set; } = new();
    public ExportConfiguration Export { get; set; } = new();
    public NetworkConfiguration Network { get; set; } = new();
    public FilterConfiguration Filter { get; set; } = new();
    public SoftwareConfiguration Software { get; set; } = new();
}

public sealed class BaiduConfiguration
{
    public string Cookie { get; set; } = string.Empty;
    public string Bduss { get; set; } = string.Empty;
    public string Stoken { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string DefaultSharePassword { get; set; } = "6666";
    /// <summary>0 表示永久有效。</summary>
    public int DefaultValidDays { get; set; }
}

public sealed class ExportConfiguration
{
    public string Directory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public string FileName { get; set; } = string.Empty;
    public bool AutoOpen { get; set; } = true;
    public bool OverwriteExisting { get; set; } = true;
}

public sealed class NetworkConfiguration
{
    public int TimeoutSeconds { get; set; } = 30;
    public int ThreadCount { get; set; } = 4;
    public int MaxRetries { get; set; } = 3;
    public int RequestIntervalMilliseconds { get; set; } = 200;
    public bool UseProxy { get; set; }
    public string ProxyAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "BaiduShareTool/0.1.0";
}

public sealed class FilterConfiguration
{
    public string IgnoredFolders { get; set; } = string.Empty;
    public string IgnoredExtensions { get; set; } = string.Empty;
    public long MinimumFileSize { get; set; }
    public long MaximumFileSize { get; set; }
}

public sealed class SoftwareConfiguration
{
    public string LogLevel { get; set; } = "Information";
    public bool AutoUpdate { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public string Theme { get; set; } = "Light";
}
