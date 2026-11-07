using Katey.Core.Charts;

namespace Katey.Core.Tests;

/// <summary>Checks the ring chart's sectors and their paths.</summary>
public sealed class PieChartTests
{
    [Fact]
    public void Build_SharesTheCircleByValue_AndSkipsWhatIsNotPositive()
    {
        var slices = PieChart.Build([("a", 50), ("b", 25), ("zero", 0), ("negative", -5), ("c", 25)], size: 220);

        Assert.Equal(["a", "b", "c"], slices.Select(slice => slice.Key));
        Assert.Equal(0.5, slices[0].Fraction);
        Assert.Equal(0, slices[0].StartAngle);
        Assert.Equal(180, slices[0].SweepAngle);
        Assert.Equal(180, slices[1].StartAngle);
        Assert.Equal(90, slices[1].SweepAngle);
        Assert.Equal(360, slices[2].StartAngle + slices[2].SweepAngle, 1e-9);
        Assert.Equal(1, slices.Sum(slice => slice.Fraction), 1e-9);
        Assert.StartsWith("M110,0 A110,110 0 0 1 110,220", slices[0].PathData, StringComparison.Ordinal);
        Assert.EndsWith("Z", slices[0].PathData, StringComparison.Ordinal);
        Assert.Contains(" 0 0 1 ", slices[1].PathData, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_DrawsAWholeRingForOneValue_AndNothingForNone()
    {
        var only = Assert.Single(PieChart.Build([("all", 7)], size: 100, thickness: 0.5));
        Assert.Equal(360, only.SweepAngle);
        Assert.Equal(2, only.PathData.Split('M').Length - 1);
        Assert.Contains("A25,25", only.PathData, StringComparison.Ordinal);

        Assert.Empty(PieChart.Build([("none", 0)]));
        Assert.Empty(PieChart.Build([]));
        Assert.Throws<ArgumentOutOfRangeException>(() => PieChart.Build([("a", 1)], size: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PieChart.Build([("a", 1)], thickness: 1.5));
    }
}
