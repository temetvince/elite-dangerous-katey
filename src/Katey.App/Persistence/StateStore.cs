using System.Text.Json;
using Katey.Core.Fleet;
using Katey.Core.Planning;
using Katey.Core.Routing;
using Katey.Core.Trading;
using Microsoft.Extensions.Logging;

namespace Katey.App.Persistence;

/// <summary>Everything the app remembers between runs.</summary>
/// <param name="Plan">Plan entries in order.</param>
/// <param name="EngineerIds">The selected engineers, by the game's engineer id.</param>
/// <param name="JournalFolder">The journal folder to watch, or <see langword="null"/> to detect it.</param>
/// <param name="Fittings">Saved fits in priority order.</param>
/// <param name="Policy">How fits share owned modules.</param>
/// <param name="RailDetached">Whether the live rail was in its own window when the app last closed.</param>
/// <param name="Preferences">Remembered view choices by name: sort orders, open groups, filters. <see langword="null"/> reads as none.</param>
/// <param name="AllowSpansh">Whether Katey may ask Spansh for routes.</param>
/// <param name="Plot">The route Spansh last plotted, or <see langword="null"/>.</param>
/// <param name="BackupFolder">Where the journal folder is backed up to, or <see langword="null"/> for nowhere.</param>
/// <param name="BackupOnClose">Whether the backup runs when the app closes.</param>
/// <param name="Reserves">The material reserves: how many of each to keep and which never to trade. <see langword="null"/> reads as none.</param>
/// <param name="AllowSpanshBodies">Whether Katey may ask Spansh about the bodies of the system the ship is in.</param>
public sealed record PersistedState(
    IReadOnlyList<PlanEntry> Plan,
    IReadOnlyList<long> EngineerIds,
    string? JournalFolder,
    IReadOnlyList<Fitting> Fittings,
    SharingPolicy Policy,
    bool RailDetached = false,
    IReadOnlyDictionary<string, string>? Preferences = null,
    bool AllowSpansh = false,
    PlottedRoute? Plot = null,
    string? BackupFolder = null,
    bool BackupOnClose = true,
    IReadOnlyList<MaterialReserve>? Reserves = null,
    bool AllowSpanshBodies = false)
{
    /// <summary>Gets a state with nothing planned, nobody selected, no folder chosen and no fits.</summary>
    public static PersistedState Empty { get; } = new([], [], null, [], SharingPolicy.Pooled);
}

/// <summary>Where the saved state lives, and the means to move it.</summary>
public interface IStateStore
{
    /// <summary>Gets where the state is kept, as text fit to show: a file path for a file store.</summary>
    string Location { get; }

    /// <summary>Reads the saved state.</summary>
    /// <returns>The state, or <see cref="PersistedState.Empty"/> when nothing is saved or the file cannot be read.</returns>
    PersistedState Load();

    /// <summary>Writes the state, replacing what was saved.</summary>
    /// <param name="state">The state.</param>
    void Save(PersistedState state);

    /// <summary>
    /// Keeps the state in a folder from now on. Nothing is copied: the caller writes the current state there
    /// when nothing is found, or loads what is found.
    /// </summary>
    /// <param name="folder">The folder. Created on the first save.</param>
    /// <returns><see langword="true"/> when the folder already holds a saved state, which <see cref="Load"/> then reads.</returns>
    bool Relocate(string folder);
}

/// <summary>Keeps the state in one JSON file, and remembers a chosen location in a small pointer file.</summary>
/// <remarks>
/// Read and write failures are logged, never thrown: losing the saved plan must not stop the app. The
/// pointer file holds the full path of the state file when the user has moved it; without one, the state
/// lives at the default path.
/// </remarks>
public sealed class JsonFileStateStore : IStateStore
{
    /// <summary>The file name the state takes in a folder the user chooses.</summary>
    public const string FileName = "katey-state.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string? _pointerPath;
    private readonly ILogger<JsonFileStateStore> _logger;

    /// <summary>Creates a store over one file.</summary>
    /// <param name="path">The JSON file. Its folder is created on first save.</param>
    /// <param name="logger">Where failures go.</param>
    /// <param name="pointerPath">The file that remembers a moved location, or <see langword="null"/> to remember none.</param>
    public JsonFileStateStore(string path, ILogger<JsonFileStateStore> logger, string? pointerPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(logger);

        Location = path;
        _logger = logger;
        _pointerPath = pointerPath;
    }

    /// <inheritdoc/>
    public string Location { get; private set; }

    /// <summary>The conventional location: <c>Katey/state.json</c> under the user's local application data.</summary>
    /// <returns>An absolute path.</returns>
    public static string DefaultPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Katey", "state.json");

    /// <summary>The conventional pointer file: <c>Katey/location.txt</c> under the user's local application data.</summary>
    /// <returns>An absolute path.</returns>
    public static string DefaultPointerPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Katey", "location.txt");

    /// <summary>Opens the store where the pointer file says the state is, or at the default path when there is no pointer.</summary>
    /// <param name="logger">Where failures go.</param>
    /// <param name="defaultPath">The state file when nothing has been moved.</param>
    /// <param name="pointerPath">The file that remembers a moved location.</param>
    /// <returns>The store. An unreadable pointer is logged and ignored.</returns>
    public static JsonFileStateStore Configured(ILogger<JsonFileStateStore> logger, string defaultPath, string pointerPath)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(pointerPath);
        var path = defaultPath;
        try
        {
            if (File.Exists(pointerPath) && File.ReadAllText(pointerPath).Trim() is { Length: > 0 } chosen)
            {
                path = chosen;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log.StateLoadFailed(logger, exception);
        }

        return new JsonFileStateStore(path, logger, pointerPath);
    }

    /// <inheritdoc/>
    public PersistedState Load()
    {
        if (!File.Exists(Location))
        {
            return PersistedState.Empty;
        }

        try
        {
            using var stream = File.OpenRead(Location);
            var state = JsonSerializer.Deserialize<PersistedState>(stream, JsonOptions);
            return state is null
                ? PersistedState.Empty
                : state with
                {
                    Plan = state.Plan ?? [],
                    EngineerIds = state.EngineerIds ?? [],
                    Fittings = state.Fittings ?? [],
                };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            Log.StateLoadFailed(_logger, exception);
            return PersistedState.Empty;
        }
    }

    /// <inheritdoc/>
    public void Save(PersistedState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Location)!);
            using var stream = File.Create(Location);
            JsonSerializer.Serialize(stream, state, JsonOptions);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Log.StateSaveFailed(_logger, exception);
        }
    }

    /// <inheritdoc/>
    public bool Relocate(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        Location = Path.Combine(Path.GetFullPath(folder), FileName);
        if (_pointerPath is not null)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_pointerPath)!);
                File.WriteAllText(_pointerPath, Location);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Log.StateSaveFailed(_logger, exception);
            }
        }

        return File.Exists(Location);
    }
}
