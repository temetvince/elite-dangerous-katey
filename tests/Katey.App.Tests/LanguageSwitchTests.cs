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
            Assert.Equal("Leichtmetalllegierung", Names.Module(modules, catalog, "anaconda_armour_grade1"));
            // A qualified variant keeps its qualifier, a named one its name, in the language's own word order.
            Assert.Equal("Mk II Großer Planetenfahrzeughangar 4F", Names.Module(modules, catalog, "int_mkiilargebuggybay_size4_class3_free"));
            Assert.Equal("Erweiterter Andockcomputer 1E", Names.Module(modules, catalog, "int_dockingcomputer_advanced"));
            Assert.Equal("Retributor 1E Fest", Names.Module(modules, catalog, "hpt_beamlaser_fixed_small_heat"));
            // The table's name wins over the journal's own localised name, so the hull reads in Katey's language, not the game's.
            Assert.Equal("Anaconda Mk II", Names.Hull("anacondamkii", "Borrowed Name"));
            Assert.Equal("Flottenträger", Names.FigureSection("FLEETCARRIER", "Fleet carrier"));
            Assert.Equal("Handelsgewinn", Names.Figure("FLEETCARRIER_TRADEPROFIT_TOTAL", "Fleet carrier trade profit total"));
            Assert.Equal("Sicherheitskräfte (Sammeltransport) befördert", Names.Figure("Passengers_Missions_SecurityForces_bulk_delivered", "x"));
            Assert.Equal("Unknown passengers (VIP) delivered", Names.Figure("Passengers_Missions_Unknown_vip_delivered", "Unknown passengers (VIP) delivered"));
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

            Assert.Equal("Сайдвиндер", Names.Hull("sidewinder", "Sidewinder"));
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
