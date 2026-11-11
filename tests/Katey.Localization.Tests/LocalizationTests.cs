using System.Globalization;

namespace Katey.Localization.Tests;

/// <summary>Templates, plural rules, culture choice and the localizer over a dictionary source.</summary>
public sealed class LocalizationTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de");
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr");
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru");
    private static readonly CultureInfo Brazilian = CultureInfo.GetCultureInfo("pt-BR");

    [Fact]
    public void Template_ParsesNamesFormatsAndEscapedBraces()
    {
        var segments = Template.Parse("Next jump {system}, {count:N0} left {{here}}");

        Assert.Equal(
        [
            TemplateSegment.Literal("Next jump "),
            TemplateSegment.Placeholder("system", null),
            TemplateSegment.Literal(", "),
            TemplateSegment.Placeholder("count", "N0"),
            TemplateSegment.Literal(" left {here}"),
        ], segments);
        Assert.Equal(["system", "count"], Template.Placeholders("{system} and {count} and {system}"));
        Assert.Equal([TemplateSegment.Literal("")], Template.Parse(""));
        Assert.Throws<FormatException>(() => Template.Parse("open {brace"));
        Assert.Throws<FormatException>(() => Template.Parse("close } brace"));
        Assert.Throws<FormatException>(() => Template.Parse("empty {} name"));
    }

    [Fact]
    public void Template_FillsByName_AndLeavesUnknownPlaceholdersAsWritten()
    {
        var text = Template.Fill("{b} before {a}, {c:0.0} unknown", (name, format) => name switch
        {
            "a" => "A",
            "b" => "B",
            _ => null,
        });

        Assert.Equal("B before A, {c:0.0} unknown", text);
    }

    [Theory]
    [InlineData("en", 0, PluralCategory.Other)]
    [InlineData("en", 1, PluralCategory.One)]
    [InlineData("en", 2, PluralCategory.Other)]
    [InlineData("de", 1, PluralCategory.One)]
    [InlineData("es", 21, PluralCategory.Other)]
    [InlineData("fr", 0, PluralCategory.One)]
    [InlineData("fr", 1, PluralCategory.One)]
    [InlineData("fr", 2, PluralCategory.Other)]
    [InlineData("pt-BR", 0, PluralCategory.One)]
    [InlineData("pt-BR", 5, PluralCategory.Other)]
    [InlineData("ru", 1, PluralCategory.One)]
    [InlineData("ru", 21, PluralCategory.One)]
    [InlineData("ru", 11, PluralCategory.Many)]
    [InlineData("ru", 2, PluralCategory.Few)]
    [InlineData("ru", 4, PluralCategory.Few)]
    [InlineData("ru", 12, PluralCategory.Many)]
    [InlineData("ru", 22, PluralCategory.Few)]
    [InlineData("ru", 5, PluralCategory.Many)]
    [InlineData("ru", 0, PluralCategory.Many)]
    [InlineData("ru", 100, PluralCategory.Many)]
    [InlineData("ru", 111, PluralCategory.Many)]
    public void PluralRules_FollowCldrForIntegers(string culture, long count, PluralCategory expected)
        => Assert.Equal(expected, PluralRules.Of(CultureInfo.GetCultureInfo(culture), count));

    [Fact]
    public void PluralRules_NameTheCategoriesAndSuffixes()
    {
        Assert.Equal([PluralCategory.One, PluralCategory.Other], PluralRules.CategoriesOf(French));
        Assert.Equal([PluralCategory.One, PluralCategory.Few, PluralCategory.Many, PluralCategory.Other], PluralRules.CategoriesOf(Russian));
        Assert.Equal(".few", PluralRules.Suffix(PluralCategory.Few));
        Assert.True(PluralRules.IsPluralForm("Route.Jumps.many", out var baseKey, out var category));
        Assert.Equal("Route.Jumps", baseKey);
        Assert.Equal(PluralCategory.Many, category);
        Assert.False(PluralRules.IsPluralForm("Route.Title", out _, out _));
        Assert.False(PluralRules.IsPluralForm(".one", out _, out _));
    }

    [Fact]
    public void Language_ResolvesASavedChoice_ThenTheMachine_ThenTheFirst()
    {
        var supported = new[] { English, German, French, Brazilian, Russian };

        Assert.Same(German, Language.Resolve("de", English, supported));
        Assert.Same(German, Language.Resolve("DE", English, supported));
        Assert.Same(French, Language.Resolve(null, CultureInfo.GetCultureInfo("fr-CA"), supported));
        Assert.Same(Brazilian, Language.Resolve("", Brazilian, supported));
        Assert.Same(English, Language.Resolve("xx", CultureInfo.GetCultureInfo("ja-JP"), supported));
        Assert.Same(English, Language.Resolve(null, CultureInfo.GetCultureInfo("pt-PT"), supported));
        Assert.Throws<ArgumentException>(() => Language.Resolve(null, English, []));
    }

    [Fact]
    public void Localizer_LooksUpFormatsAndPluralises_WithFallback()
    {
        var source = new DictionaryTextSource(new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            [""] = new Dictionary<string, string>
            {
                ["Hello"] = "Hello",
                ["Next"] = "Next jump {system}, {left} left.",
                ["Jumps.one"] = "{count} jump",
                ["Jumps.other"] = "{count} jumps",
                ["Only"] = "{count} items",
            },
            ["ru"] = new Dictionary<string, string>
            {
                ["Jumps.one"] = "{count} прыжок",
                ["Jumps.few"] = "{count} прыжка",
                ["Jumps.many"] = "{count} прыжков",
            },
        });
        var localizer = new Localizer(source);

        Assert.Equal("Hello", localizer.Get("Hello"));
        Assert.Equal("Missing.Key", localizer.Get("Missing.Key"));
        Assert.False(localizer.Has("Missing.Key"));
        Assert.Equal("Next jump Sol, 3 jumps left.", localizer.Format("Next", ("system", "Sol"), ("left", localizer.Plural("Jumps", 3))));
        Assert.Equal("1 jump", localizer.Plural("Jumps", 1));
        Assert.Equal("7 items", localizer.Plural("Only", 7));
        Assert.Equal("Nope", localizer.Plural("Nope", 2));

        Assert.Equal("21 прыжок", WithLanguage(Russian, () => localizer.Plural("Jumps", 21)));
        Assert.Equal("3 прыжка", WithLanguage(Russian, () => localizer.Plural("Jumps", 3)));
        Assert.Equal("Hello", WithLanguage(Russian, () => localizer.Get("Hello")));
        Assert.Equal(["Hello", "Jumps.one", "Jumps.other", "Next", "Only"], source.Keys(CultureInfo.InvariantCulture).Order(StringComparer.Ordinal));
        Assert.Equal(3, source.Keys(Russian).Count);
    }

    [Fact]
    public void Localizer_WritesValuesInTheFormattingCulture()
    {
        var source = new DictionaryTextSource(new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            [""] = new Dictionary<string, string> { ["N"] = "{value:N1} ly" },
        });
        var localizer = new Localizer(source);
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = German;
            Assert.Equal("1.234,5 ly", localizer.Format("N", ("value", 1234.5)));
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Equal("1,234.5 ly", localizer.Format("N", ("value", 1234.5)));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void PseudoTextSource_LengthensAndAccents_ButKeepsPlaceholders()
    {
        var text = PseudoTextSource.Transform("Next jump {system}, {count:N0} left");

        Assert.StartsWith("[Néxt júmp {system}, {count:N0} léft ~", text, StringComparison.Ordinal);
        Assert.EndsWith("]", text, StringComparison.Ordinal);
        Assert.Equal("", PseudoTextSource.Transform(""));
        Assert.Equal("[únbálánçéd { ~~~]", PseudoTextSource.Transform("unbalanced {"));
    }

    [Fact]
    public void Language_SetRaisesChangedAndMovesTheUiCulture()
    {
        var before = CultureInfo.CurrentUICulture;
        var defaultBefore = CultureInfo.DefaultThreadCurrentUICulture;
        var raised = 0;
        Language.Changed += OnChanged;
        try
        {
            Language.Set(German);
            Assert.Equal(1, raised);
            Assert.Same(German, Language.Current);
        }
        finally
        {
            Language.Changed -= OnChanged;
            CultureInfo.CurrentUICulture = before;
            CultureInfo.DefaultThreadCurrentUICulture = defaultBefore;
        }

        void OnChanged(object? sender, EventArgs args) => raised++;
    }

    private static T WithLanguage<T>(CultureInfo culture, Func<T> read)
    {
        var before = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;
        try
        {
            return read();
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }
}
