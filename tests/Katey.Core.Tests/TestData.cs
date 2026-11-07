namespace Katey.Core.Tests;

/// <summary>The fixtures embedded in this assembly: two EDSY exports.</summary>
internal static class TestData
{
    /// <summary>An Anaconda Mk II with eleven engineered modules.</summary>
    public static string Anaconda => Read("anaconda.json");

    /// <summary>A Mandalay with two engineered modules.</summary>
    public static string Mandalay => Read("mandalay.json");

    private static string Read(string file)
    {
        using var stream = typeof(TestData).Assembly.GetManifestResourceStream($"Katey.Core.Tests.TestData.{file}")
            ?? throw new InvalidOperationException($"Missing fixture {file}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
