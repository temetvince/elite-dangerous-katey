using System.Globalization;
using Katey.App.Text;
using Katey.Core;
using Katey.Core.Fleet;
using Katey.Localization;

namespace Katey.App.Tests;

/// <summary>The words follow the language switch: a translation is read, plurals take the language's form, and the game's names come from the names table or are composed from it.</summary>
public sealed class LanguageSwitchTests
{
    [Fact]
    public void German_ReadsTheTables_AndComposesTheGameNames()
    {
        var catalog = Catalog.LoadEmbedded();
        var modules = ModuleCatalog.LoadEmbedded();
        var before = CultureInfo.CurrentUICulture;
        var defaultBefore = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            Language.Set(CultureInfo.GetCultureInfo("de"));

            Assert.Equal("Module", Strings.Get(Keys.Nav_Modules));
            Assert.Equal("1 Sprung", Strings.Plural(Keys.Plural_Jumps, 1));
            Assert.Equal("3 Sprünge", Strings.Plural(Keys.Plural_Jumps, 3));
            Assert.Equal("Eisen", Names.Material(catalog.Materials["Iron"]));
            Assert.Equal("Welt mit hohem Metallgehalt", Names.PlanetClass("High metal content body"));
            Assert.Equal("dünne Atmosphäre aus Kohlendioxid", Names.Atmosphere("thin carbon dioxide atmosphere"));
            Assert.Equal("geringer Vulkanismus mit Wassermagma", Names.Volcanism("minor water magma volcanism"));
            Assert.Equal("Vulkanismus mit Wassermagma", Names.Volcanism("water magma volcanism"));
            Assert.Equal("Frameshift-Antrieb 5A", Names.Module(modules, catalog, "int_hyperdrive_size5_class5"));
            Assert.Equal("thick  atmosphere", Names.Atmosphere("thick  atmosphere"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
            CultureInfo.DefaultThreadCurrentUICulture = defaultBefore;
            Language.Set(before);
        }
    }

    [Fact]
    public void Russian_TakesFourPluralForms()
    {
        var before = CultureInfo.CurrentUICulture;
        var defaultBefore = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            Language.Set(CultureInfo.GetCultureInfo("ru"));
            var one = Strings.Plural(Keys.Plural_Jumps, 1);
            var few = Strings.Plural(Keys.Plural_Jumps, 3);
            var many = Strings.Plural(Keys.Plural_Jumps, 5);

            Assert.StartsWith("1 ", one, StringComparison.Ordinal);
            Assert.StartsWith("3 ", few, StringComparison.Ordinal);
            Assert.StartsWith("5 ", many, StringComparison.Ordinal);
            Assert.NotEqual(one[2..], few[2..]);
            Assert.NotEqual(few[2..], many[2..]);
            Assert.Equal(many[2..], Strings.Plural(Keys.Plural_Jumps, 11)[3..]);
            Assert.Equal(one[2..], Strings.Plural(Keys.Plural_Jumps, 21)[3..]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
            CultureInfo.DefaultThreadCurrentUICulture = defaultBefore;
            Language.Set(before);
        }
    }
}
