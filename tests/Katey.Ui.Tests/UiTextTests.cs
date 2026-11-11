using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Katey.App.Text;
using Katey.Localization;
using Katey.Ui.Text;

[assembly: AvaloniaTestApplication(typeof(Katey.Ui.Tests.TestApp))]

namespace Katey.Ui.Tests;

/// <summary>The headless Avalonia application the binding tests run in.</summary>
public static class TestApp
{
    /// <summary>Configures a headless Avalonia with no rendering.</summary>
    /// <returns>The builder.</returns>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

/// <summary>The chrome's string table, its keys, its translations, and the markup extension that binds XAML to it.</summary>
public sealed partial class UiTextTests
{
    [GeneratedRegex(@"\{loc:T\s+([A-Za-z0-9_.]+)", RegexOptions.CultureInvariant)]
    private static partial Regex Usage();

    [GeneratedRegex(@"UiKeys\.([A-Za-z0-9_]+)", RegexOptions.CultureInvariant)]
    private static partial Regex CodeUsage();

    [Fact]
    public void Keys_MatchTheEnglishTable()
        => Assert.Empty(TableChecks.KeyProblems(UiText.Localizer.Source, Constants(typeof(UiKeys))));

    [Fact]
    public void EveryLanguage_HasEveryKeyAndPlaceholder()
        => Assert.Empty(TableChecks.TranslationProblems(UiText.Localizer.Source, Languages.Supported.Skip(1)));

    [Fact]
    public void EveryKeyTheXamlUses_IsInTheTable_AndEveryKeyIsUsed()
    {
        var root = Path.Combine(SourceRoot(), "src", "Katey.Ui");
        var used = Directory.EnumerateFiles(root, "*.axaml", SearchOption.AllDirectories)
            .SelectMany(file => Usage().Matches(File.ReadAllText(file)).Select(match => match.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);
        var table = UiText.Localizer.Source.Keys(CultureInfo.InvariantCulture);
        var fromCode = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(file => CodeUsage().Matches(File.ReadAllText(file)).Select(match => match.Groups[1].Value.Replace('_', '.')))
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(used.Count > 0, "No {loc:T} usage found; is the source root right?");
        Assert.Empty(used.Except(table).Order(StringComparer.Ordinal));
        Assert.Empty(table.Except(used).Except(fromCode).Order(StringComparer.Ordinal));
    }

    [AvaloniaFact]
    public void TExtension_BindsAKey_AndFollowsTheLanguage()
    {
        var before = CultureInfo.CurrentUICulture;
        var defaultBefore = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            Language.Set(CultureInfo.GetCultureInfo("en"));
            var block = new TextBlock();
            BindText(block, UiKeys.Chrome_Back, upper: false);
            var upper = new TextBlock();
            BindText(upper, UiKeys.Bar_Plan, upper: true);

            Assert.Equal("← Back", block.Text);
            Assert.Equal("PLAN", upper.Text);

            // A collection between two switches must not silence the texts: the window holds nothing but the binding.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Language.Set(CultureInfo.GetCultureInfo("de"));
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Assert.Equal("← Zurück", block.Text);
            Assert.Equal("PLAN", upper.Text);
            Language.Set(CultureInfo.GetCultureInfo("en"));
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Assert.Equal("← Back", block.Text);
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
            CultureInfo.DefaultThreadCurrentUICulture = defaultBefore;
            Language.Set(before);
        }
    }

    // In its own frame, so nothing but the control itself can keep the bound text alive once this returns.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void BindText(TextBlock block, string key, bool upper)
        => block.Bind(TextBlock.TextProperty, new TExtension(key) { Upper = upper }.ProvideValue(new TargetOnly(block)));

    /// <summary>The XAML services as the extension sees them, reduced to the object the text is set on.</summary>
    private sealed class TargetOnly(object target) : IServiceProvider, Avalonia.Markup.Xaml.IProvideValueTarget
    {
        public object TargetObject => target;

        public object TargetProperty => TextBlock.TextProperty;

        public object? GetService(Type serviceType) => serviceType == typeof(Avalonia.Markup.Xaml.IProvideValueTarget) ? this : null;
    }

    private static IEnumerable<string> Constants(Type keysType)
        => keysType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral).Select(field => (string)field.GetRawConstantValue()!);

    private static string SourceRoot([CallerFilePath] string path = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
}
