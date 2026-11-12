using System.Globalization;
using System.Reflection;
using Katey.App.Text;
using Katey.Core;
using Katey.Localization;

namespace Katey.App.Tests;

/// <summary>
/// The application's string table and its translations agree with each other and with the generated keys:
/// every language has every key, every placeholder, and every plural form its grammar needs.
/// </summary>
public sealed class StringTableTests
{
    [Fact]
    public void Keys_MatchTheEnglishTable()
        => Assert.Empty(TableChecks.KeyProblems(Strings.Localizer.Source, Constants(typeof(Keys))));

    [Fact]
    public void EveryLanguage_HasEveryKeyAndPlaceholder()
        => Assert.Empty(TableChecks.TranslationProblems(Strings.Localizer.Source, Languages.Supported.Skip(1)));

    [Fact]
    public void EveryLanguage_NamesEveryGameThing()
    {
        var expected = CatalogNameKeys.All(Catalog.LoadEmbedded());
        var problems = Languages.Supported.Skip(1).SelectMany(culture => TableChecks.KeyProblems(Names.Localizer.Source, culture, expected)).ToList();

        Assert.Empty(problems);
    }

    [Fact]
    public void Names_FallBackToTheDataInEnglish()
    {
        var catalog = Catalog.LoadEmbedded();
        var modules = Core.Fleet.ModuleCatalog.LoadEmbedded();

        Assert.Equal("Iron", Names.Material(catalog.Materials["Iron"]));
        Assert.Equal("Increased range", Names.Blueprint(catalog.Blueprints["FSD_LongRange"]));
        Assert.Equal(modules.NameOf("int_hyperdrive_size5_class5"), Names.Module(modules, catalog, "int_hyperdrive_size5_class5"));
        Assert.Equal("High metal content body", Names.PlanetClass("High metal content body"));
        Assert.Equal("thin carbon dioxide atmosphere", Names.Atmosphere("thin carbon dioxide atmosphere"));
        Assert.Equal("minor water magma volcanism", Names.Volcanism("minor water magma volcanism"));
        Assert.Equal("Bounty hunting profit", Names.Figure("Bounty_Hunting_Profit", "Bounty hunting profit"));
        Assert.Equal("Lightweight Alloy", Names.Module(modules, catalog, "anaconda_armour_grade1"));
        Assert.Equal("MkII Large Planetary Vehicle Hangar 4F", Names.Module(modules, catalog, "int_mkiilargebuggybay_size4_class3_free"));
        Assert.Equal("Anaconda Mk II", Names.Hull("anacondamkii", null));
        Assert.Equal("Borrowed Name", Names.Hull("anacondamkii", "Borrowed Name"));
        Assert.Equal("Security forces passengers (bulk) delivered", Names.Figure("Passengers_Missions_SecurityForces_bulk_delivered", "Security forces passengers (bulk) delivered"));
    }

    [Fact]
    public void Languages_OfferTheMachineFirst_AndNameThemselves()
    {
        var options = Languages.Options("");

        Assert.Null(options[0].Culture);
        Assert.Equal("", options[0].Saved);
        Assert.True(options[0].IsCurrent);
        Assert.Equal("English", options[1].Label);
        Assert.False(options[1].IsCurrent);
        Assert.Equal("Português (Brasil)", Languages.NativeName(CultureInfo.GetCultureInfo("pt-BR")));
        Assert.Equal("Deutsch", Languages.NativeName(CultureInfo.GetCultureInfo("de")));
    }

    /// <summary>The string constants a generated keys class declares.</summary>
    /// <param name="keysType">The class.</param>
    /// <returns>The values.</returns>
    public static IEnumerable<string> Constants(Type keysType)
        => keysType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral).Select(field => (string)field.GetRawConstantValue()!);
}
