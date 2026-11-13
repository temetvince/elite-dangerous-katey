# Katey

A desktop companion for Elite Dangerous that reads the game's own journal files and keeps up with you as
you play. It plans engineering against your live material inventory, follows your plotted route and the
system you are in, and checks your saved ship fits against the ships and modules you actually own.

It runs on Windows and on Linux, where Elite Dangerous runs through Proton. Nothing is sent anywhere
unless you allow it in Settings, where outgoing connections are off until you say otherwise.

## Quick start

```sh
dotnet build Katey.slnx
dotnet run --project src/Katey.Ui
```

Or download a release: each one is a self-contained single-file executable, so nothing else needs installing.
On first run Katey finds the journal folder in its usual place, replays your journal history once, and then
follows the newest file while the game writes it. The [manual](MANUAL.md) walks through every page and switch,
starting with a quick start for players. Katey writes `katey.log` beside its saved data, with folder paths and
account names left out; start it with `--verbose`, or `--log-level <level>`, for a fuller log when chasing a
bug. The manual's Your data section says what the log holds.

## How the journal is read

Katey polls the journal folder about once a second. On start it replays every journal file, oldest first, into
nine trackers:

- material counts;
- route position and visited systems;
- the current system;
- the fleet;
- recent activity;
- the statistics: the game's lifetime figures, an income ledger, the income shared with wings and crews, and
  the Powerplay standing;
- the technology broker purchases;
- the engineering rolls, which move the plan on as you engineer;
- the exploration and organic data aboard and unsold, and what each sale paid.

A line is only parsed when a tracker wants its event, which is what keeps a multi-year history to a few
seconds.

While that replay runs the window shows its progress and nothing else that is live, so you never see a
position or a figure from partway through the history; everything appears together when it finishes. It then
follows the newest file for new lines and re-reads `NavRoute.json` and `Status.json` whenever the game
rewrites them. The [journal library reference](src/Katey.Journal/README.md) lists the events each tracker uses
and what the game does and does not write.

Three limits follow from what the game writes:

- Whether someone else has discovered a system is only known once you scan a body in it. Ahead of
  arrival, Katey can only say whether *you* have been there.
- The fleet list and the stored-module list are rewritten when you dock somewhere with a shipyard or
  outfitting, so they are as fresh as your last such visit. A stored module's blueprint and grade are
  recorded but not its experimental effect, which the fit checker reports as uncertain.
- A prebuilt ship you have never deployed does not exist to the game, so it cannot appear in the fleet.

## How it is put together

Five projects, each one layer more specific than the last. Dependencies only ever point from specific to
general.

| Project | Role |
| --- | --- |
| [`src/Katey.Localization`](src/Katey.Localization/README.md) | The language mechanism with no words of its own: keyed lookups over a text source, templates filled by placeholder name, plural rules per language, and the one language switch. |
| [`src/Katey.Core`](src/Katey.Core/README.md) | The domain: the data catalogs, a name index, engineering plans and totals, the technology broker unlocks and their budget, engineer coverage, EDSY import, star classes and positions, cartographics and exobiology estimates, the fit matcher, the fuel model and the route plotting types. |
| [`src/Katey.Journal`](src/Katey.Journal/README.md) | Reading journal files and the live files, the seven trackers, and the folder backup. Knows nothing about engineering. |
| [`src/Katey.App`](src/Katey.App/README.md) | The pages, the rail, the plan drawer, the search, the Spansh client, the saved state, and the words in six languages. No UI toolkit dependency, so it is unit-tested as plain objects. |
| [`src/Katey.Ui`](src/Katey.Ui/README.md) | The Avalonia window and the rail's own window over `Katey.App`, with the chrome's words in six languages. |

Core and Journal do not reference each other; `Katey.App` joins them. `Katey.Localization` references nothing
else and is referenced by App and Ui. The data tables, their provenance
and their licensing are described in the [Core reference](src/Katey.Core/README.md#the-data-tables) and
in [NOTICE.md](NOTICE.md).

## Languages

Katey runs in English, German, Spanish, French, Portuguese (Brazil) and Russian. It follows the machine's
language until you pick one from the globe at the left of the bottom bar; the choice is saved, and the whole
window changes at once. The game's own names change with it: materials, blueprints, experimental effects,
module types, technology broker items, planet classes, atmospheres and volcanism, and the lifetime figures. The
manual exists in every language too, as `MANUAL.<culture>.md` beside the English one, each linked from the top
of the others. The licence notices, `NOTICE.md` and `THIRD-PARTY-NOTICES.txt`, stay in the language their
authors wrote them in: a licence binds in its published wording, and a translation would be a paraphrase with
no standing.

### Translating Katey

The words live in three tables per language, plain `.resx` files you edit with any resx editor; Visual Studio
opens them as a grid, and the free ResX Resource Manager extension shows every language side by side.

| Table | Holds |
| --- | --- |
| `src/Katey.App/Text/Strings.<culture>.resx` | Every sentence and label the pages, the rail and the plan produce. |
| `src/Katey.Ui/Text/UiStrings.<culture>.resx` | The window's headings, buttons and notices. |
| `src/Katey.App/Text/CatalogNames.<culture>.resx` | The game's names, keyed by material symbol, blueprint key, module type code, unlock key, planet class, atmosphere word and so on. |

Each English entry carries a comment saying where the text shows and how long it may run. The rules:

- A placeholder such as `{count}` or `{system}` stays exactly as written; move it wherever the sentence needs
  it. `{{` and `}}` are literal braces.
- A counted text is a group of keys ending in `.one` and `.other`; Russian adds `.few` and `.many`. French and
  Portuguese use `.one` for 0 and 1.
- The game's names should match the game's own localisation where you know it.

`dotnet test Katey.slnx` reports every gap in plain words: a key missing from a language, an extra key, a
placeholder that differs from the English, a plural form the language needs, a template that does not parse.
To see where text clips before a translation exists, start Katey with `--pseudo-loc`: every word comes back
longer and accented.

`publish.bat` at the root builds one Windows executable, `katey.exe`, into `bin/`: self-contained and
compressed, so nothing needs installing on the machine it runs on. `bash publish.sh` does the same from a
Linux machine, still producing the Windows executable. Beside the executable it places the manual,
`NOTICE.md` and `THIRD-PARTY-NOTICES.txt`; those files are part of every release. Debug symbols and
documentation files never ship.

Pushing a tag that starts with `v` builds a release. The workflow in `.github/workflows/release.yml` runs on
Linux: it builds and tests the solution, checks its whitespace formatting, runs `publish.sh`, and attaches the
resulting `katey.exe`, with the manual and the notices, as a zip archive to a GitHub release named after the
tag. On the runner, compiler and analyzer warnings stay warnings rather than failing the build, since the
runner's SDK can be newer than the one the code was developed with; a local build still treats every warning
as an error, which is where they are fixed. It is the only workflow, so an ordinary push is not checked; the
gate runs when a tag is pushed, before anything is published. Releases are Windows only; on Linux, build from
source with the quick start above.

```sh
git tag v1.0.0
git push origin v1.0.0
```

The tag's number becomes the build's version, which Katey names in every request it makes and compares with
the latest release when the release check is on. The publish
scripts take the same number from `KATEY_VERSION` and use 1.0.0 without it.

Trimming is left off on purpose: the window's bindings use reflection.

## Tests

```sh
dotnet test Katey.slnx
```

The suites are described in [tests/README.md](tests/README.md). The docs have their own gate: the markdown lint
and `dotnet run --project tools/CheckDocLinks`, which fails on a link or heading anchor that no longer resolves.

## Repository layout

| Path | Contents |
| --- | --- |
| `src/` | The five projects above. |
| `tests/` | Five test projects. |
| `tools/` | `StringKeys.cs`, the file-based program that writes the key constants of a `.resx` table, and [`CheckDocLinks`](tools/CheckDocLinks/README.md), which checks every link and anchor across the docs. |
| `Directory.Build.props` | Repo-wide build settings, including the warnings-as-errors gate. |
| `.github/workflows/` | The release workflow, triggered by version tags. |
| `.editorconfig` | Analyzer rules, code style and naming. The source of truth for both. |
| `AGENTS.md` | The working agreement for changes made here. |
| `MANUAL.md`, `MANUAL.<culture>.md` | The user manual for players, with a quick start first, in English and in every shipped language. All shipped beside the executable. |
| `NOTICE.md` | Katey's licence, every licence of the software compiled into it, and the credits and terms of the game data. Shipped beside the executable. |
| `THIRD-PARTY-NOTICES.txt` | The long third-party notices of the .NET runtime and of SkiaSharp and HarfBuzzSharp, as their publishers ship them. Shipped beside the executable. |

## Thanks

Special thanks to [alexalsp](https://github.com/alexalsp) for the feedback and bug reports that improved
Katey, and for improving its Russian translation.
