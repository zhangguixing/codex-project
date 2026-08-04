using BaiduShareTool.Providers.Abstractions;
using BaiduShareTool.Providers.Simulation;
using Xunit;

namespace BaiduShareTool.Tests;

public sealed class SimulationProviderTests
{
    [Fact]
    public async Task SimulationProvider_ReturnsThreeFiles()
    {
        var provider = new InMemoryStorageProvider();
        var files = new List<RemoteFile>();
        await foreach (var file in provider.EnumerateFilesAsync("/示例", new ScanOptions())) files.Add(file);
        Assert.Equal(3, files.Count);
    }
}
