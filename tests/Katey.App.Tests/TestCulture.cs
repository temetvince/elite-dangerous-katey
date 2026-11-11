using System.Globalization;
using System.Runtime.CompilerServices;

// The language switch is process-wide, so tests that flip it cannot run beside tests that read words.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Katey.App.Tests;

/// <summary>
/// Pins the cultures the tests run in: English words from the neutral table, and invariant formatting, so a
/// test can assert a whole sentence whatever the machine's regional settings are.
/// </summary>
internal static class TestCulture
{
    /// <summary>Runs once when the test assembly loads, before any test.</summary>
    [ModuleInitializer]
    internal static void Pin()
    {
        var english = CultureInfo.GetCultureInfo("en");
        CultureInfo.DefaultThreadCurrentUICulture = english;
        CultureInfo.CurrentUICulture = english;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }
}
