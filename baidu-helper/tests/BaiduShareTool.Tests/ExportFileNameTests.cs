using BaiduShareTool.Export;
using Xunit;

namespace BaiduShareTool.Tests;

public sealed class ExportFileNameTests
{
    [Theory]
    [InlineData("功夫熊猫.mp4", "功夫熊猫")]
    [InlineData("abc.tar.gz", "abc.tar")]
    [InlineData("README", "README")]
    [InlineData(".config", ".config")]
    public void RemoveLastExtension_UsesOnlyLastExtension(string input, string expected)
        => Assert.Equal(expected, OpenXmlExcelExportService.RemoveLastExtension(input));
}
