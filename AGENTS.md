# AGENTS.md — working agreement for C# work

Standing instructions for any AI assistant making changes in this repository. This file is committed
to git so it travels with the code and is loaded automatically at the start of every session in this folder.
**Follow every rule below on every change, without being reminded**, wherever you are running.

This is a general-purpose working agreement for a C#/.NET codebase. Where a rule references tooling
(`dotnet build`, `.editorconfig`, project layout), adapt the specifics to what this repository actually uses;
the principle behind the rule always holds. The **Project specifics** section at the bottom holds this
repo's real solution name, run/test commands, and layout.

## The rules

Apply all of these to every non-trivial change, as part of "done" — not optional polish.

### 1. Modularity — decompose every addition; separate the reusable core from the specifics

**Split every non-trivial addition into its reusable core and its local specifics.** Before writing a feature,
separate its **invariant** — the part that would be the same in any application of this kind (a mechanism, an
algorithm, a lifecycle, a data shape, a base type) — from its **variant** — *this* application's specific
rules, content, UX, and configuration. Push the invariant as far down as it will go, into the most reusable
place it belongs (a shared library or core project), and leave the calling code only the variant, plugged in
through interfaces, hooks, or generic parameters. Done consistently, the shared core grows more capable with
every feature and stays reusable for the next consumer, while the outer layers stay thin — the specifics,
never the machinery. This is a primary design goal; weigh it on every change.

**Where the invariant goes — most reusable first: core/shared library, then the consuming project.** Put a
neutral, unopinionated mechanism (a utility, a data structure, an algorithm) in the lowest reusable layer even
if only one caller uses it today. Put an opinionated convention that removes recurring boilerplate (a base
class, a pipeline, a lifecycle host) in the framework/shared layer that owns that convention, and let each
consumer plug in its specifics through hooks. Only what is genuinely specific to one consumer stays in that
consumer's project.

**Extract the invariant, not the variant.** "As much logic as possible in the shared core" means as much of
the *genuinely reusable* part as possible — never cram application-specific UX, content, or rules into a
shared library just to make it bigger. When a thing is mostly application-flavoured (one screen's layout and
strings, one feature's business rules), keep it in the consuming project and lift only the reusable kernel it
rests on. When the invariant/variant line is genuinely unclear, that is a rule-7 trade-off — surface it rather
than guess.

**Structural constraints (always).** Keep dependencies flowing one way, from specific to general — a shared
core never references its consumers, and sibling consumers never reference each other. Keep concerns that must
stay decoupled in separate assemblies (e.g. a pure logic layer free of UI/IO so it can be referenced and
tested in isolation). A framework/base type takes generic or already-prepared values, not a consumer's raw
concrete types.

### 2. Refactor, don't bolt on

Favor refactoring toward the right design even when it is a lot of work — including **creating new
projects/assemblies** when that is what modularity (rule 1) calls for. Match the surrounding patterns and
naming. Do not paper over a design problem with a local patch when the clean fix is a refactor.

### 3. Tests

Write tests wherever they add regression value; **mock where possible**. **All tests must pass** — do not
delete or skip a failing test to go green unless it is genuinely obsolete, and if so, say why. Put pure,
deterministic behaviour in fast unit tests; reserve slower end-to-end / integration tests for behaviour that
genuinely spans components.

**Unit tests are the priority; integration tests must never ossify the code.** Favor fast, focused unit
tests. And **never keep production code worse — a suboptimal structure, a misplaced type, a leaked concept —
just to avoid changing an integration test.** When the right design breaks an integration test, change the
code to be as good as it can be, then adapt, rewrite, move, replace, or remove the integration test to fit the
new design. The integration test serves the code, never the reverse.

### 4. Zero warnings — no analyzer squiggles

Leave **no** compiler warnings and **no** analyzer "this could be improved" suggestions (the yellow
squiggles). Analyzer rules and severities live in [`.editorconfig`](.editorconfig) (portable, read by the
build and every editor) — that is the source of truth, not editor-only settings. Verify with a warning-free
`dotnet build` and `dotnet format` (which applies the whitespace, style, **and** analyzer fixers per
`.editorconfig`). If a warning is truly a false positive, suppress it narrowly with justification rather than
leaving it visible. Prefer a build that treats warnings as errors so a squiggle fails the local build, not just
the editor. The release workflow is the one place warnings stay warnings, so that a newer SDK on the runner
cannot block a release with a rule this code has not met; that is not licence to leave one, and a warning seen
in a release log is fixed next.

### 5. Docs — update them with the change (if relevant)

Docs ship with the change, never as a follow-up. The structure and its **single-source rule**:

| Surface | Role | Style |
| --- | --- | --- |
| Per-project `README.md` | the **reference** for that project (types, API, file map); travels with the code | technical |
| Solution `README.md` | the **guide** for the solution as a whole | instructional |

A guide links *down* to the project READMEs; it never restates them. Every fact lives in exactly one place.
Markdown must pass the repo's markdown lint rules (compact tables — never hand-align pipes — sensible line
length, etc.) and any doc link-check.

**Write docs to be read, not decoded — clarity beats brevity.** The goal is prose an engineer new to the code
can follow on the first pass, not the fewest possible words. Concision is a virtue only up to the point where
it starts hiding meaning; past it, a "dense" sentence is a defect, not a flourish. Specifically:

- **One thought per sentence.** If a sentence chains a whole pipeline, lifecycle, or decision tree through
  arrows, semicolons, em-dashes, and stacked parentheticals, it is doing too much — break it into several
  short sentences. As a rule of thumb, more than one nested parenthetical or more than ~35 words is a smell.
- **Use structure for structured content.** A sequence of steps is an ordered list; a set of independent
  things (config knobs, message kinds, options) is a bullet list or a table — not a run-on sentence with
  `→`/`;` separators. Reserve inline prose for genuine narrative ("why", "how it fits").
- **Parentheticals are the exception, not the sentence.** A clause important enough to keep belongs in its own
  sentence; if it is not important enough for that, cut it. Never nest them.
- **Keep the facts, drop the compression.** Rewriting for clarity must not delete technical content or move a
  fact to a second location (single-source still holds) — it re-shapes the *same* information into readable
  form. Expanding a wall of text into a list or a few sentences is always the right trade here.

**Document the present, not the past.** Docs describe what the code does *now* — never what it used to do, how
it changed, or how to migrate from an older design. No "breaking changes", "previously", "the old behavior",
"changed from", before/after narration, or migration/changelog sections in a README or XML doc; that history
lives in git, not in the reference. Write every behavioural caveat as a present-tense fact about how the code
works today.

Apply this when you write new docs and when you touch an existing dense passage: leave the prose clearer than
you found it.

### 6. Format the whole solution

Before finishing, run `dotnet format` over the **entire** solution, then rebuild and run the tests. The result
must be green with zero warnings.

### 7. Ask the user on trade-off decisions

When a task involves a genuine choice — an architecture or design decision with real trade-offs, an ambiguous
requirement, or more than one reasonable approach — **ask the user before committing to a direction** rather
than guessing. Lay out the options and their trade-offs concisely (a recommendation plus the alternatives) and
let them decide. This matters most for choices that are expensive to reverse: project/solution structure,
public APIs, data or wire formats, dependencies, and cross-cutting patterns. For low-stakes or clearly
conventional choices, pick the sensible default, state it, and move on — do not stall on trivia.

### 8. Add logging where appropriate

Instrument new code through the project's logging abstraction (e.g. `Microsoft.Extensions.Logging` — a
per-class `ILogger<T>`, then `LogDebug/LogInformation/LogWarning/LogError/LogCritical`) so a running process
can be understood from its logs without a debugger. Log the things worth knowing: lifecycle and state
transitions (startup/shutdown, connect/disconnect, mode changes), decisions worth an audit trail (rejected
input, retries, fallbacks), and failures — errors with the exception. Pick the level by audience and match the
surrounding code: `Information` for milestones a human watches, `Debug` for the fine-grained trail,
`Warning`/`Error`/`Critical` for trouble. **Do not log inside a hot path or tight inner loop** (it floods the
log and stalls the process); if a repeating condition must be logged, throttle it (log the first, then every
Nth). Never log secrets, credentials, or personal data.

### 9. Document by contract

Every public type, method, and property carries an XML doc comment — a `<summary>`, plus a `<param>` for each
parameter, a `<returns>`, and a `<typeparam>` for each type parameter — unless `<inheritdoc/>` genuinely
covers it (an override or interface implementation that adds nothing new). Write them **by contract**, not as
narration: state the **preconditions** a caller must satisfy, the **postconditions** the member guarantees,
and the **invariants** the type holds across its lifetime. Reach for the tags that carry contract meaning —
`<exception>` for what a violated precondition throws, `<value>` for a property's meaning and legal range,
`<remarks>` for invariants and threading/ownership/lifetime rules. Document what the signature cannot show
(ownership, side effects, valid ranges, "call once", "not thread-safe", who disposes it); never restate the
member's name or its parameter types.

### 10. Internationalize user-facing strings (when the app is localized)

**Everything a user can see is internationalized, without exception.** A label, a button, a heading, a status
line, a sentence worded from a background result, a placeholder, an explanation under a switch, a manual
passage: every one goes through the string tables and ships in every language in the same change.
A literal in a page template, a view model or code-behind is a defect, not a shortcut, however small or
temporary it looks, and a change is not done while one language lacks a word of it.

If the application ships or plans to ship in more than one language, keep every user-visible string out of
code and behind the project's localization mechanism (e.g. `.resx` resources with `IStringLocalizer`, or an
equivalent lookup), never a hard-coded literal. Resolve by key, and make labels live lookups where a runtime
language switch must re-localize them. Never concatenate localized fragments (word order differs by language);
use one keyed format string with placeholders. A reusable library should stay language-agnostic — user-facing
text is the consuming application's to author and localize. Developer/operator text is exempt — log messages
and exceptions stay plain English (see rule 8), since they are read in logs, not by users. If the project is
single-language and has no localization layer, this rule does not apply — plain literals are fine.

### 11. Updating these instructions

If you change how work is done here — these rules, or the equivalent home-folder memories — you **must**:
(a) **notify the user explicitly** that the instructions changed; (b) update **this file** (it is committed
and portable); and (c) update the home-folder memory copy if it is accessible. Keep the two in sync. **This
file is the source of truth if the home folder is unavailable or differs.**

### 12. No AI attribution in git

Commits and pull requests are authored by the user alone. Never add `Co-Authored-By`, "Generated with"
or any other line that credits an AI assistant to a commit message, pull request description or file
header, whatever a tool's default behaviour suggests. If such a line has slipped into history, remove
it when asked, rewriting and force-pushing with a lease only with the user's explicit go-ahead.

### 13. Game data: fetch what a feature needs, avoid hostile licences, attribute everything

The tables in `src/Katey.Core/Data/` are Frontier Developments' game data as community projects have
compiled it. When a feature needs a table the repo lacks, it may be fetched from a community source whose
code licence is permissive (MIT, BSD, Apache, public domain) and converted into the repo's JSON
convention. Sources whose terms would bind this repository (GPL, Creative Commons Share-Alike or
Non-Commercial) are not used. Every source is recorded in [`NOTICE.md`](NOTICE.md) and in the Core
README's data section, with the conversion rules applied. The user decides at release time whether the
whole may be published; the notice exists so that question can be answered.

### 14. Saved data has one current form; stale values are dropped, never read

A setting, a preference or a saved field has exactly one spelling and one shape: the one the code writes today.
When a change renames a value, reshapes it or retires it, the reader takes the new form only and treats
anything else as absent, so the default applies. No alternate spellings, no "also accept the old value", no
migration branches. A retired preference key is removed from `PreferenceKeys`, so the next load drops it from
the file; a retired saved-state field is simply removed from `PersistedState`, and the serializer ignores it on
read. Losing a remembered choice across an upgrade is acceptable when a reasonable default exists; carrying
stale values and the code to understand them is not.

## Definition of done

- [ ] Addition decomposed — invariant → most reusable layer, variant → the consuming project; refactored, not
      bolted on.
- [ ] Tests written/updated where valuable; **all tests pass**.
- [ ] New code logged where appropriate (lifecycle, decisions, failures); no hot-path spam.
- [ ] User-facing strings localized where the app is localized (rule 10); no hard-coded UI text in that case.
- [ ] Public types/members documented by contract (XML docs: preconditions, postconditions, invariants).
- [ ] `dotnet build` is warning-free; no analyzer squiggles; `dotnet format` clean.
- [ ] Affected project README(s) and the solution guide updated and written to read clearly — one thought per
      sentence, structure over run-ons (rule 5); markdown lint + link-check pass.
- [ ] Every language has the change: the five string tables, the five names tables where game data changed,
      and the five manuals where `MANUAL.md` changed (project specifics, "What every change translates").
- [ ] Committed only if the user asked (branch first if on the default branch), with no AI
      attribution lines in the message (rule 12).

## Where config lives

| Concern | Home |
| --- | --- |
| Warning gate (TreatWarningsAsErrors, analyzers, code-style in build) | `Directory.Build.props` (root; imported by every project) |
| Roslyn analyzer rules + severities, naming, whitespace | [`.editorconfig`](.editorconfig) (portable; the source of truth) |
| Editor behavior (format-on-save, code actions) | editor settings (e.g. `.vscode/settings.json`) — display only |
| Markdown rules | [`.markdownlint.json`](.markdownlint.json) |

Prefer a root `Directory.Build.props` that sets `TreatWarningsAsErrors`, enables the .NET analyzers, and turns
on `EnforceCodeStyleInBuild`, so a squiggle fails `dotnet build`/CI rather than only showing in the editor.
Scope any deliberate exceptions in [`.editorconfig`](.editorconfig) with a justification comment; prefer
fixing the code over adding more.

## Project specifics

- **Solution:** `Katey.slnx` (.NET 10). The app is called Katey; the repository is `ed-katey`.
- **Build:** `dotnet build Katey.slnx`.
- **Run:** `dotnet run --project src/Katey.Ui`.
- **Test:** `dotnet test Katey.slnx`.
- **Docs gate:** `npx --yes markdownlint-cli2 "**/*.md" "#node_modules" "#bin" "#obj"` and
  `dotnet run --project tools/CheckDocLinks`, which checks every relative link and heading anchor across the
  Markdown and every backtick-wrapped symbol link's target. Both run whenever a `.md` file changes.
- **Publish locally:** `publish.bat` on Windows, or `bash publish.sh` on Linux, writes one self-contained,
  compressed Windows `katey.exe` into `bin/`, with every `MANUAL*.md`, `NOTICE.md` and
  `THIRD-PARTY-NOTICES.txt` beside it; the release zip carries the same files. `KATEY_VERSION` stamps the
  build's version, which the release workflow takes from the tag. `NOTICE.md` is generated content in
  spirit: when a package, a font or a data source changes, update its section and copyright lines there.
  Each script passes `-p:PublishDocumentationFile=false -p:PublishReferencesDocumentationFiles=false` to
  keep the XML out of the output, never `GenerateDocumentationFile=false`. If a framework-dependent build
  is ever wanted, use `--no-self-contained`, not `--self-contained false`, which the SDK ignores, and leave
  ReadyToRun off: a framework-dependent ReadyToRun single file starts and never shows its window.
- **What every change translates.** Katey ships in English, German, Spanish, French, Portuguese (Brazil)
  and Russian, and a change is not done until every language has it:
  - a new or changed user-facing string lands in `Strings.resx` or `UiStrings.resx` and in all five
    `*.<culture>.resx` beside it, with its plural forms (`.few` and `.many` for Russian);
  - a new material, material family, blueprint, effect, module type, unlock, surface signal type, planet
    class, atmosphere or volcanism word, or lifetime figure lands in all five `CatalogNames.<culture>.resx`;
  - a change to `MANUAL.md` is made in the five `MANUAL.<culture>.md` in the same change, with the same
    structure and the UI's own words from the string tables;
  - English is the source of truth. `Strings.resx`, `UiStrings.resx`, `MANUAL.md` and the READMEs are written
    first and the translations follow them; where a translation and the English disagree, the English is right
    and the translation is corrected to it.
  - `NOTICE.md` and `THIRD-PARTY-NOTICES.txt` are never translated: a licence binds in its published
    wording, and the third-party file is its publishers' own text reproduced verbatim.
  `StringTableTests`, `UiTextTests` and `ManualTests` fail on a gap; the docs gate checks the manuals' links.
- **Release:** push a `v*` tag; `.github/workflows/release.yml` runs on Linux, builds and tests with
  warnings kept as warnings, checks whitespace formatting, reports the full format check without blocking,
  runs `publish.sh` and attaches the Windows `katey.exe` as a zip to a GitHub release. Windows
  is the only release target; keep the publish flags in `publish.sh` and `publish.bat`, never in the
  workflow. It is the only workflow: ordinary pushes are not checked, so run the build, tests and
  `dotnet format` locally before pushing. Do not pass `GenerateDocumentationFile=false` to publish: the
  analyzer gate needs it on.
- **Layout:**
  - `src/Katey.Core` — the domain: the engineering catalog loaded from the embedded JSON tables and
    validated, a text index, galactic positions, plan entries, material totals and shortfall, engineer
    coverage, EDSY import, star classes, the cartographics and exobiology estimates, the module catalog,
    the fit matcher, the fuel model, the route plotting types, the material trader and the technology
    broker unlocks with their budget. No UI, no file IO beyond its embedded resources, no network.
  - `src/Katey.Localization` — the language mechanism with no words: keyed lookups over a text source,
    templates filled by placeholder name, plural rules per language, the one language switch, and the
    checks a table must pass. References nothing else.
  - `src/Katey.Journal` — reads Elite Dangerous journal files and the live `NavRoute.json` and
    `Status.json`, keeps nine trackers current: materials, route and visited systems, system, fleet,
    activity, statistics, technology broker purchases, engineering rolls, data aboard and sold; and mirrors the
    journal folder into a backup folder. Knows nothing about engineering. Does not reference Core.
  - `src/Katey.App` — the shell: a `Workspace` of shared state, a `Navigator` over entity pages
    (`Pages/`), the live `RailViewModel`, the `PlanDrawerViewModel`, the `SearchViewModel`, the Spansh
    client behind `IRoutePlotter` (`Routing/`), the backup state (`Backup/`), the material reserves
    (`Trading/`), and the saved state. No UI
    toolkit dependency. References both.
  - `src/Katey.Ui` — the Avalonia main window and the rail's own window over Katey.App. Code-behind holds
    only platform concerns: opening and closing the rail window, the caption buttons and the title-bar
    drag, the folder picker, the clipboard and the search box's keys.
  - `tests/Katey.Core.Tests`, `tests/Katey.Journal.Tests`, `tests/Katey.App.Tests`,
    `tests/Katey.Localization.Tests` — xUnit 2, with hand-written fakes rather than a mocking library.
    `tests/Katey.Ui.Tests` — xUnit 3 under headless Avalonia, which that package requires.
  - `tools/StringKeys.cs` — the file-based program that writes a table's key constants.
    `tools/CheckDocLinks` — the doc link checker, in the solution so the warning gate covers it.
- **Non-obvious:**
  - The data tables are JSON with widened types, so `Catalog.FromRows` runs `CatalogValidator` and throws
    `CatalogException` listing every problem. `CatalogTests` loads the embedded tables, which is what makes
    a data typo fail the test run.
  - `ItemResolver` matches EDSY item symbols against each module type's regular expressions; when several
    match, the longest pattern wins. That is how a bi-weave shield generator beats the plain one.
  - `JournalMonitor` polls rather than using `FileSystemWatcher`: the game appends to its journal without
    signalling, and a notification would still have to be followed by a read. It replays the whole history
    into every `IJournalSink`, parsing a line only when a sink wants its event, which is what keeps a
    multi-year history to seconds.
  - Logs go to `katey.log` beside the default `state.json` through `FileLoggerProvider`, Information and
    above unless the command line's `--log-level <level>` names another floor, or `--verbose` asks for
    debug, and to the Debug output only while a debugger is attached. Log messages never carry a folder: a
    path is logged as its file name or not at all, and `LogScrubber` strips the folders from any path that
    still reaches the file, an exception message say, and replaces the account and machine names. Never log
    a commander name, credits or a position. `Program.Main` writes an unhandled exception to the file before
    the process ends, so a crash report starts with that file, not with the Windows event log.
  - `Workspace` receives a dispatcher delegate; the monitor raises events on a background thread and the
    workspace marshals every event it raises through it. While the monitor is catching up, every
    snapshot the workspace hands out is empty, which is what keeps the replay off the screen; read
    trackers through the workspace, never through `Sinks` directly, in anything that shows live state.
    Trackers lock internally and hand out immutable snapshots. Pages do not subscribe to anything: the
    navigator rebuilds a page on `Open` and calls `Refresh` on the current one whenever the shell sees a
    change.
  - Every mention of an entity on a page is a `Link` carrying a `PageKey`; `PageFactory` turns a key into
    a page, or a `NotFoundPage` when the id names nothing. Add a new page kind in `PageKind`, the factory,
    and a data template in `MainWindow.axaml`.
  - A system named inside a sentence goes through `Phrase.Of` with a `Link` and is shown by
    `PhraseBlock`, never an interpolated string, so every system name in Katey opens its page; a
    structured place is a `Place`. A new sentence that names a system follows the same rule.
  - An engineer named anywhere is an `EngineerLink` from `Workspace.EngineerLinkFor`, shown through
    the application's data template or as a toned `PhrasePart`, so the tick and the engineers switch
    colour every name alike: plain, the HUD blue when ticked while the switch is on, dimmed when not.
  - The rail's `IsDetached` is view-model state; the main window reacts to it by opening or closing
    `RailWindow` over the same `RailViewModel`. The window's overlay, click-through and remembered place
    are preferences on the view model too; `RailWindow` applies them, dropping the system border while
    clicks fall through, and click-through is a Win32 extended style (`WS_EX_TRANSPARENT`) set from its
    code-behind, a no-op elsewhere. On open, `RailWindow` applies the decorations before the remembered
    bounds: changing `WindowDecorations` resizes the client area at once and Avalonia writes that size into
    `Width` and `Height`, so a size set before it is lost. `RailView` paints its ground only as far as its
    content, so the overlay is clear below the last line.
  - The game targets the next jump by itself on every arrival along a route, so a destination in
    `Status.json` says nothing about whether the player is exploring here; the rail shows the bodies
    from arrival and the route only while the drive charges in supercruise, a jump has begun from
    normal space or the ship is in hyperspace, since a charge in normal space may be for
    supercruise. Jumping to a system off the plotted route makes the game re-plot through it and
    report `RemainingJumpsInRoute` with the target, but it does not rewrite `NavRoute.json`, so
    `RoutePage.RerouteJumps` takes the game's count over the stale file's stops and `RerouteLine`
    says so.
  - Colour by meaning is a resource pair in `App.axaml`, one per theme, reached from XAML through the
    `SemanticBrush` converter with a key prefix (`KG` + grade, `K` + `MaterialCategory`, `K` +
    `ModuleGroup`) or `StarBrush` for a star class. A new meaning gets a resource pair and a `README`
    line in `Katey.Ui`, never a hex literal in a page template. Verdicts are `Border.badge` with a tone
    class; figures are `TextBlock.stat` and `ProgressBar.meter`.
  - `UiPreferences` keeps only the keys `PreferenceKeys` lists, on load and on reload, so a key no longer
    written leaves the saved file at the next start. A new preference key is added to that registry with
    the code that uses it; `sort.` and `section.` are open prefixes, every other key is exact.
  - Tables sort through `TableSort<T>`, long pages switch sections through `SectionSwitch`, collapsible
    groups open through `GroupExpansion`, and all three remember themselves through `UiPreferences`,
    which is saved with the state. A new table gets a sort with a preference key; a new multi-section
    page gets a switch; a new grouped page gets an expansion.
  - The saved data file can be moved: `Katey/location.txt` beside the default `state.json` holds the
    path of a moved file, and `JsonFileStateStore.Configured` reads it on start. Moving saves to the old
    place first, then loads a file already in the new folder or writes the current data there.
  - `LangVersion` is pinned in `Directory.Build.props`, not `latest`: the IDE's compiler can be newer than
    the SDK's and would otherwise offer syntax such as `[with(comparer), …]` that the command-line build
    rejects. Do not accept an IDE "simplify" that introduces a feature the pinned version lacks.
  - `PlanEntry` is two things in one record: a module to engineer (blueprint, grade, rolls, experimental)
    or a technology broker unlock (`Unlock` set, blueprint empty, grade 0). `IsUnlock` tells them apart
    and every consumer must branch on it before touching the blueprint; the material calculator, the
    engineer coverage and the plan view models already do. `UnlockBudget` subtracts the whole enabled plan
    from the inventory before judging an unlock, which is what makes ticking one dim another.
  - The plan follows the game's engineering: `PlanProgress` applies each `EngineerCraft` to the first
    module entry of its blueprint and type, counting rolls down and retiring the entry at its target with
    its effect. `PlanEntry.CraftsAfter` is the anchor that keeps the replayed history off an entry and
    stops a roll being applied twice; `Create` sets it to now, `PlanViewModel.Load` anchors an entry saved
    without one, and tests that feed dated rolls set it explicitly.
  - `FitMatcher` matches within one fit first (a module is used once per fit) and then applies the sharing
    policy across fits: pooled never reserves, same-hull reserves against other hulls, none reserves
    against everyone. Among equal matches it takes a module fitted to the fit's own hull, then a stored
    one, then one on another hull, so a fit is met by its own ship before it borrows; match quality
    decides before place, and wasted engineering after it. It and `FitComparer` compare items through
    `ModuleSymbolNames.Canonical`: the game writes `_free` on a bundled module and a hull code on some
    cargo hatches (`modularcargobaydoorfdl`), EDSY writes neither. A new spelling the game invents belongs
    there, not in a page.
  - Identity is never a display name. Materials are keyed by their journal symbol in every table,
    lookup, page key and saved value, and engineers by the game's engineer id in page keys and the
    saved selection, since the journal spells a name differently from the tables (`Tod 'The Blaster'
    McQuinn`). `Catalog.Engineers` stays keyed by name because the module tables name who offers a
    grade; `EngineerPage.KeyOf` and `Find` turn an id into a page key and back. A display name is
    shown, never looked up.
  - Katey is localized, so rule 10 applies in full. Every user-facing string in `Katey.App` is a key in
    `Text/Strings.resx` read through `Strings.Get`, `Strings.Format`, `Strings.Plural` or `Phrase.Format`;
    every one in `Katey.Ui` is a `{loc:T Key}` binding on `Text/UiStrings.resx` or a `UiText.Get`. A new
    sentence gets a key with a comment, never a literal, and a sentence with two shapes gets two keys;
    fragments are never concatenated. `Keys.cs` and `UiKeys.cs` are generated by
    `dotnet run tools/StringKeys.cs -- <table> <out> <namespace> <class>` and a drift test fails until they
    are regenerated. Counted nouns are plural groups, `Plural.X.one` and `.other`, with `.few` and `.many`
    for Russian. Every language table must be complete; `StringTableTests` and `UiTextTests` list the gaps.
    `Languages.Supported` and the `SatelliteResourceLanguages` list in `publish.bat` and `publish.sh` name
    the shipped cultures and must agree.
  - The game's names go through `Names`, whose `CatalogNames.resx` has an empty neutral file: English is
    the data's own word, so `Names` falls back to the catalog, the journal or `LabelOf` and never shows a
    key. A translation must hold every key `CatalogNameKeys.All` lists. A module's name in a translated
    language is composed from its type's name, class, rating and mount; in English it is the module
    catalog's. Core and Journal stay language-agnostic: a reason or a kind leaves them as an enum
    (`ShortfallReason`, `EdsyParseError`, `StarFamily`) and the application words it.
  - `tests/Katey.App.Tests` pins the UI culture to English and the formatting culture to invariant in a
    module initializer, which is why its sentence asserts hold on any machine. Numbers and dates in
    templates use `CultureInfo.CurrentCulture`; keys, ids, URLs and preference values stay invariant.
  - `ExobiologyPredictor` canonicalises the journal's and Bioforge's spellings of atmospheres, body types
    and volcanism to one key before comparing; a new category name that slips past that shows up as a
    uniformly low score, not an error.
  - `.editorconfig` is the template's, unchanged in substance: every analyzer reports at warning, and
    `TreatWarningsAsErrors` turns each into a build failure locally. `Directory.Build.props` sets it only when
    nothing else has, and the release workflow sets it to false in its environment, so the runner's SDK, which
    can be newer than the one here, reports its new rules as warnings instead of failing the release. The
    workflow checks whitespace formatting hard and the full `dotnet format` check as a report only.
    IDE0072 and IDE0010, add missing switch cases, are off: their fixer fills each case it adds with
    `throw new NotImplementedException()`, so a `dotnet format` run would turn a fallback arm into a crash.
    Name every case of an enum switch yourself and let the compiler's CS8509 catch a missing one. The
    other rules `.editorconfig` turns off, each with its reason beside it, are the fixers that reshape code
    rather than correct it: folding blocks into expressions, null checks into `?.` and `??`, lambdas into
    method groups, and IDE0130, whose fix crashes the formatter. Both shapes of a member are allowed; the
    author chooses, the formatter leaves it.
  - Katey has no tooltips. Avalonia's tooltip is a popup window of its own, and on Windows that window takes
    the click meant for the control under it, however the tip is placed. A hint goes in visible text: a
    muted line under the control, a status line, or the manual. `ToolTip.Tip` does not appear in the XAML.
  - The main window's minimum width is the pages' minimum, 960, plus the docked rail's 340 while the rail
    is docked; `MainWindow` applies it when the rail docks or pops out. Fit a narrower screen by popping
    the rail out, not by shrinking the pages.
  - The main window has two bars: the top one carries the wordmark, back and forward, the page links and
    the caption buttons and is the drag strip; the bottom one carries the language picker, the settings
    link, the Stats link, the search box and the plan button, with the search results rising above the
    box. Stats sits below so the page links fit one line in the longest language.
  - The windows draw their own title bars: `WindowDecorations="BorderOnly"` with
    `ExtendClientAreaToDecorationsHint`, caption buttons with plain `Click` handlers, and `WindowDrag` on
    pointer press. Avalonia 12's `WindowDecorationProperties.ElementRole` does nothing for a Win32 window
    in this mode, so none of the chrome relies on it. The drawn title bar Avalonia adds under
    `WindowDecorations="Full"` overlaps the content until the first resize, which is why Full is not used.
    `WindowDecorations="None"` with hand-drawn resize grips was tried and dropped: a window with no system
    frame could not be resized through `BeginResizeDrag` or by moving its own bounds from a pointer drag.
  - Nothing leaves the machine unless `NetworkAccess` allows it: a Spansh plot behind `IRoutePlotter`, a
    Spansh system lookup behind `ISystemCatalog` and a GitHub release check behind `IReleaseFeed`, each on
    its own switch, all off by default. A new kind of request gets its own switch there, its own interface,
    and a line in the settings text and the manual, and keeps the README's promise true. A request made at
    startup runs on the thread pool from its first instruction and never delays the window; `UpdateState`
    shows the shape.
  - A state object that learns something in the background keeps the facts, a record or an exception, and
    words them only in a property read on the UI thread, so a language switch re-words the line without
    another request. `BackupState`, `SystemBodiesState` and `UpdateState` all follow this; a new one does too.
