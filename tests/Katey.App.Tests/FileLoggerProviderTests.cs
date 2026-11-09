using Katey.App.Logging;
using Microsoft.Extensions.Logging;

namespace Katey.App.Tests;

public sealed class FileLoggerProviderTests
{
    [Fact]
    public void WritesLinesAtOrAboveTheMinimum_RollsOver_AndRecordsACrash()
    {
        var folder = Path.Combine(Path.GetTempPath(), "katey-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "katey.log");
        try
        {
            using var provider = new FileLoggerProvider(path, LogLevel.Information, rollOverBytes: 1024, scrub: text => text.Replace("secret", "~", StringComparison.Ordinal));
            Assert.Equal(LogLevel.Information, provider.Minimum);
            var logger = provider.CreateLogger("Katey.Tests");
            Assert.False(logger.IsEnabled(LogLevel.Debug));
            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.False(logger.IsEnabled(LogLevel.None));

            logger.Log(LogLevel.Debug, default, "not written", null, (state, _) => state);
            logger.Log(LogLevel.Information, default, "started 3 things", null, (state, _) => state);
            logger.Log(LogLevel.Error, default, "failed", new InvalidOperationException("boom in secret"), (state, _) => state);
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("secret", text, StringComparison.Ordinal);
            Assert.Contains("boom in ~", text, StringComparison.Ordinal);
            Assert.DoesNotContain("not written", text, StringComparison.Ordinal);
            Assert.Contains("[Information] Katey.Tests: started 3 things", text, StringComparison.Ordinal);
            Assert.Contains("[Error] Katey.Tests: failed", text, StringComparison.Ordinal);
            Assert.Contains("InvalidOperationException: boom", text, StringComparison.Ordinal);

            // Past the roll-over size the file is set aside as .old and a fresh one begins.
            for (var i = 0; i < 40; i++)
            {
                var padding = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"line {i} padding padding padding padding padding");
                logger.Log(LogLevel.Information, default, padding, null, (state, _) => state);
            }

            Assert.True(File.Exists(path + ".old"));
            Assert.True(new FileInfo(path).Length < new FileInfo(path + ".old").Length + 1024);

            provider.WriteCrash(new InvalidOperationException("down"));
            Assert.Contains("[Critical] Katey: The process is ending on an unhandled exception.", File.ReadAllText(path), StringComparison.Ordinal);
            Assert.Null(logger.BeginScope("scope"));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
