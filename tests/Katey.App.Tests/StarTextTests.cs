using Katey.App.ViewModels;
using Katey.Core.Exploration;

namespace Katey.App.Tests;

/// <summary>A star class in a reader's words.</summary>
public sealed class StarTextTests
{
    [Theory]
    [InlineData("K", "Class K star")]
    [InlineData("M_RedGiant", "Class M red giant star")]
    [InlineData("K_OrangeGiant", "Class K orange giant star")]
    [InlineData("A_BlueWhiteSuperGiant", "Class A blue-white supergiant star")]
    [InlineData("L", "Class L brown dwarf")]
    [InlineData("TTS", "T Tauri star")]
    [InlineData("WN", "Wolf-Rayet star, class WN")]
    [InlineData("N", "Neutron star")]
    [InlineData("DA", "White dwarf, class DA")]
    [InlineData("H", "Black hole")]
    [InlineData("Y", "Class Y brown dwarf")]
    [InlineData("X", "Class X star")]
    [InlineData(null, "Star")]
    public void Describe_PutsAClassInWords(string? starClass, string expected)
        => Assert.Equal(expected, StarText.Describe(starClass));

    [Theory]
    [InlineData("M_RedGiant", "M red giant")]
    [InlineData("K_SomeNewGiant", "K some new giant")]
    [InlineData("K", "K")]
    [InlineData(null, "")]
    public void Brief_NamesTheGiant(string? starClass, string expected)
        => Assert.Equal(expected, StarText.Brief(starClass));

    [Fact]
    public void Label_NamesWhatTheStarMeansToTheShip()
    {
        Assert.Equal("scoopable", StarText.Label(StarKind.Scoopable));
        Assert.Equal("neutron star", StarText.Label(StarKind.Neutron));
        Assert.Equal("not scoopable", StarText.Label(StarKind.Other));
    }
}
