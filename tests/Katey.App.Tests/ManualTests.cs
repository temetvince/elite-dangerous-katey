using System.Runtime.CompilerServices;
using Katey.App.Text;

namespace Katey.App.Tests;

/// <summary>Every language Katey ships in has its manual, and each manual links to the others.</summary>
public sealed class ManualTests
{
    [Fact]
    public void EveryLanguage_HasAManual_ThatLinksToTheOthers()
    {
        var root = SourceRoot();
        var manuals = Languages.Supported.Skip(1).Select(culture => $"MANUAL.{culture.Name}.md").Prepend("MANUAL.md").ToList();
        var missing = manuals.Where(name => !File.Exists(Path.Combine(root, name))).ToList();
        Assert.Empty(missing);

        foreach (var name in manuals)
        {
            var text = File.ReadAllText(Path.Combine(root, name));
            var unlinked = manuals.Where(other => other != name && !text.Contains($"({other})", StringComparison.Ordinal)).ToList();
            Assert.True(unlinked.Count == 0, $"{name} does not link to {string.Join(", ", unlinked)}.");
        }
    }

    private static string SourceRoot([CallerFilePath] string path = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
}
