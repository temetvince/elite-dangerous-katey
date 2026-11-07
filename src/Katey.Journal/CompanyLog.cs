using System.Text.Json;

namespace Katey.Journal;

/// <summary>Who the player is flying with.</summary>
public enum Company
{
    /// <summary>A wing of up to four ships.</summary>
    Wing,

    /// <summary>A multicrew session, as helm or as crew.</summary>
    Crew,
}

/// <summary>One stretch flown with other players.</summary>
/// <param name="Company">A wing or a crew.</param>
/// <param name="Partners">Everyone who was in it at some point, in the order they appeared.</param>
/// <param name="Start">When the player joined or the first partner arrived.</param>
/// <param name="End">When it ended, or <see langword="null"/> while it is still open.</param>
public sealed record CompanySession(Company Company, IReadOnlyList<string> Partners, DateTimeOffset Start, DateTimeOffset? End)
{
    /// <summary>How long the session has run.</summary>
    /// <param name="asOf">The time to measure an open session to; ignored for a closed one.</param>
    /// <returns>The length, never negative.</returns>
    public TimeSpan Length(DateTimeOffset asOf)
    {
        var end = End ?? asOf;
        return end > Start ? end - Start : TimeSpan.Zero;
    }
}

/// <summary>
/// Follows the wing and crew events to say, at any point in the history, whether the player is flying with
/// others and with whom, and keeps every session flown.
/// </summary>
/// <remarks>
/// A wing opens at <c>WingJoin</c> or <c>WingAdd</c> and closes at <c>WingLeave</c>. A crew opens at
/// <c>JoinACrew</c> or <c>CrewMemberJoins</c> and closes at <c>QuitACrew</c>, <c>EndCrewSession</c> or when the
/// last crew member leaves. <c>LoadGame</c> and <c>Shutdown</c> close both, since a crash writes no leave
/// event. Not thread-safe: the owner locks.
/// </remarks>
public sealed class CompanyLog
{
    /// <summary>The events the log reads.</summary>
    public static IReadOnlySet<string> Events { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "WingJoin", "WingAdd", "WingLeave", "JoinACrew", "QuitACrew", "CrewMemberJoins", "CrewMemberQuits",
        "KickCrewMember", "EndCrewSession", "LoadGame", "Shutdown",
    };

    private readonly List<CompanySession> _closed = [];
    private OpenSession? _wing;
    private OpenSession? _crew;

    /// <summary>Gets whether the player is in a wing.</summary>
    public bool InWing => _wing is not null;

    /// <summary>Gets whether the player is in a crew, as helm with at least one crew member or as crew.</summary>
    public bool InCrew => _crew is not null;

    /// <summary>Gets the wingmates present now, in the order they appeared; empty outside a wing.</summary>
    public IReadOnlyList<string> WingPartners => _wing is null ? [] : [.. _wing.Current];

    /// <summary>Gets the crew present now, or the captain when the player is crew; empty outside a crew.</summary>
    public IReadOnlyList<string> CrewPartners => _crew is null ? [] : [.. _crew.Current];

    /// <summary>Every session, oldest first, open ones last.</summary>
    /// <returns>A new list.</returns>
    public IReadOnlyList<CompanySession> Sessions()
    {
        var list = new List<CompanySession>(_closed);
        if (_wing is not null)
        {
            list.Add(_wing.Snapshot(null));
        }

        if (_crew is not null)
        {
            list.Add(_crew.Snapshot(null));
        }

        return list;
    }

    /// <summary>Forgets everything.</summary>
    public void Reset()
    {
        _closed.Clear();
        _wing = null;
        _crew = null;
    }

    /// <summary>Reads one event.</summary>
    /// <param name="name">The event name.</param>
    /// <param name="payload">The event.</param>
    /// <param name="at">Its timestamp.</param>
    /// <returns><see langword="true"/> when the company changed.</returns>
    public bool Apply(string name, JsonElement payload, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(name);
        return name switch
        {
            "WingJoin" => Join(ref _wing, Company.Wing, at, JsonRead.Items(payload, "Others").Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : null)),
            "WingAdd" => Join(ref _wing, Company.Wing, at, [JsonRead.Text(payload, "Name")]),
            "WingLeave" => Close(ref _wing, at),
            "JoinACrew" => Join(ref _crew, Company.Crew, at, [JsonRead.Text(payload, "Captain")]),
            "CrewMemberJoins" => Join(ref _crew, Company.Crew, at, [JsonRead.Text(payload, "Crew")]),
            "CrewMemberQuits" or "KickCrewMember" => Leave(JsonRead.Text(payload, "Crew"), at),
            "QuitACrew" or "EndCrewSession" => Close(ref _crew, at),
            "LoadGame" or "Shutdown" => Close(ref _wing, at) | Close(ref _crew, at),
            _ => false,
        };
    }

    private static bool Join(ref OpenSession? session, Company company, DateTimeOffset at, IEnumerable<string?> names)
    {
        session ??= new OpenSession(company, at);
        var changed = false;
        foreach (var name in names)
        {
            if (name is { Length: > 0 })
            {
                changed |= session.Add(name);
            }
        }

        return changed || session.Start == at;
    }

    private bool Leave(string? name, DateTimeOffset at)
    {
        if (_crew is null || name is not { Length: > 0 })
        {
            return false;
        }

        _crew.Remove(name);
        return _crew.Current.Count != 0 || Close(ref _crew, at);
    }

    private bool Close(ref OpenSession? session, DateTimeOffset at)
    {
        if (session is null)
        {
            return false;
        }

        _closed.Add(session.Snapshot(at));
        session = null;
        return true;
    }

    private sealed class OpenSession(Company company, DateTimeOffset start)
    {
        private readonly List<string> _all = [];

        public DateTimeOffset Start { get; } = start;

        public List<string> Current { get; } = [];

        public bool Add(string name)
        {
            if (Current.Contains(name, StringComparer.Ordinal))
            {
                return false;
            }

            Current.Add(name);
            if (!_all.Contains(name, StringComparer.Ordinal))
            {
                _all.Add(name);
            }

            return true;
        }

        public void Remove(string name) => Current.Remove(name);

        public CompanySession Snapshot(DateTimeOffset? end) => new(company, [.. _all], Start, end);
    }
}
