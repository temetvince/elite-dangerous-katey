using Katey.App.Logging;
using Microsoft.Extensions.Logging;

namespace Katey.App.Tests;

public sealed class LogArgumentsTests
{
    [Theory]
    [InlineData(new string[0], LogLevel.Information)]
    [InlineData(new[] { "--log-level", "debug" }, LogLevel.Debug)]
    [InlineData(new[] { "--verbose" }, LogLevel.Debug)]
    [InlineData(new[] { "--verbose", "--log-level", "warning" }, LogLevel.Warning)]
    [InlineData(new[] { "--log-level=Warning" }, LogLevel.Warning)]
    [InlineData(new[] { "--LOG-LEVEL", "info" }, LogLevel.Information)]
    [InlineData(new[] { "--log-level", "trace", "--log-level", "error" }, LogLevel.Error)]
    [InlineData(new[] { "--log-level", "loud" }, LogLevel.Information)]
    [InlineData(new[] { "--log-level" }, LogLevel.Information)]
    [InlineData(new[] { "--other", "debug" }, LogLevel.Information)]
    public void FileLevel_ReadsTheLogOption(string[] args, LogLevel expected)
        => Assert.Equal(expected, LogArguments.FileLevel(args));
}
