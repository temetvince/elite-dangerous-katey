namespace Katey.Core.Tests;

/// <summary>Checks that EDSY item symbols land on the right module type, variants included.</summary>
public sealed class ItemResolverTests
{
    private static readonly ItemResolver Resolver = new(Catalog.LoadEmbedded());

    [Theory]
    [InlineData("int_hyperdrive_overcharge_size8_class5", "fsd")]
    [InlineData("Int_Hyperdrive_Size5_Class5", "fsd")]
    [InlineData("int_engine_size7_class5", "t")]
    [InlineData("int_mkiiagileboost_engine_size5_class5", "t")]
    [InlineData("int_fighterbaymk2_size5_class1_free", "fh")]
    [InlineData("int_mkiilargebuggybay_size4_class3", "pv")]
    [InlineData("int_mkii_passengercabin_size4_class2", "pci")]
    [InlineData("hpt_xenoscannermk2_basic_tiny", "xs")]
    [InlineData("int_hyperdrive_size8_class5", "fsd")]
    [InlineData("int_shieldgenerator_size8_class3_fast", "bsg")]
    [InlineData("int_shieldgenerator_size4_class5", "sg")]
    [InlineData("int_shieldgenerator_size4_class5_strong", "psg")]
    [InlineData("anacondamkii_armour_grade3", "bh")]
    [InlineData("hpt_multicannon_gimbal_huge", "mc")]
    [InlineData("hpt_multicannon_fixed_medium_advanced", "advmc")]
    [InlineData("hpt_basicmissilerack_fixed_medium", "mr")]
    [InlineData("hpt_drunkmissilerack_fixed_medium", "mr")]
    [InlineData("hpt_dumbfiremissilerack_fixed_medium", "dmr")]
    [InlineData("hpt_dumbfiremissilerack_fixed_medium_lasso", "dmr")]
    [InlineData("hpt_dumbfiremissilerack_fixed_medium_advanced", "amr")]
    [InlineData("hpt_atdumbfiremissile_fixed_large_v2", "axmre")]
    [InlineData("hpt_atmulticannon_gimbal_large", "axmce")]
    [InlineData("int_corrosionproofcargorack_size4_class1", "cr")]
    [InlineData("int_largecargorack_size7_class1", "crl")]
    [InlineData("int_multidronecontrol_operations_size3_class3", "mlc")]
    [InlineData("int_dronecontrol_recon_size1_class1", "rcpl")]
    [InlineData("int_missing_hyperdrive", "mm")]
    public void Resolve_FindsTheModuleType(string item, string expected)
        => Assert.Equal(expected, Resolver.Resolve(item));

    [Fact]
    public void Resolve_ReturnsNullForAnUnknownItem()
        => Assert.Null(Resolver.Resolve("int_totally_made_up_size1"));
}
