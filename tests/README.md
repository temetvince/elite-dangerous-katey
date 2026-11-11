# Tests

```sh
dotnet test Katey.slnx
```

Five suites, each covering the project it is named after.

| Project | Covers |
| --- | --- |
| `Katey.Localization.Tests` | Templates with named placeholders and escaped braces, the plural rules for every shipped language, culture resolution from a saved choice and the machine, the localizer over a dictionary source with its fallbacks, formatting in the formatting culture, the pseudo-locale, and the language switch raising its event. |
| `Katey.Core.Tests` | The embedded catalogs and their validation, item resolution, the cross-link queries, the text index, galactic distances, material sums and shortfall including experimental-only entries, engineer coverage, the EDSY parser against two real exports and the EDSY writer, star classes, the cartographics estimate, the species predictor, the module catalog and its cosmetic filter, the fit matcher's three policies, the fit comparer including modules found in other slots of the same kind, hull and slot names and slot kinds, the fuel model and projection, the routing types against the shape of Spansh's replies, the ring chart's sectors, the technology broker unlocks and their validation, and the unlock budget that follows the plan. |
| `Katey.Journal.Tests` | Journal line parsing, item symbol normalisation, inventory replay for every applied event, the route, exploration, fleet and activity trackers against hand-written lines including recent jumps, fuel capacity, the loadout's masses and modifiers, the status file's cargo, the statistics tracker's lifetime figures, income ledger, shared ledger with its wing and crew sessions, and Powerplay standing, the route tracker forgetting its jumps when the drive changes, the fleet tracker marking a redeemed ship and the modules it came with, the unlock tracker recording technology broker purchases, the craft tracker recording every roll with its grade progress and effect, the fleet tracker applying a roll to the fitted module, the folder backup against a temporary folder, and the monitor against a temporary folder: replay, tailing, switching to a new file, and the live files. |
| `Katey.App.Tests` | The shell with a fake store and a fake monitor that feeds lines to the real trackers: restoring saved state, adding from a module page, engineer selection and coverage, import, inventory laid against the drawer and the material pages, the settings page and the watch, the fleet pages including a pasted fit replacing a saved one of the same name and hull only after confirmation, navigation history and scroll memory, search, the rail, the JSON state store, remembered sorts, sections, groups and toggles, experimental-only entries, the fit comparison, the ship page's pickers and EDSY copy, cosmetic filtering, fuel along the route, the Spansh plot with a fake plotter: the switch, the request built from the ship or from a saved fit, the picker's order with the chosen target and the ship being flown first and its search, the waypoints, progress along them, failure, clearing and restoring; the journal backup from settings and on close with a fake backup; the statistics page's chart, switches, period, company, exobiology, Powerplay, wings and crews, and lifetime figures; adding to the plan from a blueprint page; the engineers' coverage groups; the catch-up gate; moving the saved data, both with a fake store and through the file store's pointer file; the material reserves set on the materials page and the trades they drive on the page and in the drawer; the prebuilt-module warning on the ship and fit pages; and the unlocks page: ticking an unlock adds a plan entry, the budget dims what the plan puts out of reach and brings it back, a bought unlock cannot be ticked, the drawer lists the commodities, and a saved unlock entry comes back on restart; and the plan following the game's engineering: rolls counting down, an entry leaving when its grade and effect are reached, a roll from before the entry ignored, and an old save anchored on load. |
| `Katey.Ui.Tests` | Under headless Avalonia: the `{loc:T}` binding reads a key and re-evaluates on a language switch; and, as plain tests, that every key the XAML and code-behind use is in the chrome's table and every key in it is used, that the generated constants match the table, and that every language is complete. |

## The load-bearing tests

- **The data gate.** `CatalogTests` loads the embedded tables and asserts their sizes, then rebuilds them
  with a misspelt material, a misspelt engineer, an unused engineer, a bad grade and a bad region, and
  asserts each is reported. A typo in `src/Katey.Core/Data/` fails here.
- **The two exports.** `EdsyImportTests` and `FleetTests` parse the Anaconda and Mandalay fixtures in
  `TestData/` and assert the module counts, types and effects the import must find.
- **Live following.** `JournalMonitorTests` writes journal files into a temporary folder, starts a monitor
  with a 40 ms poll, appends lines, creates a newer file, drops in `NavRoute.json` and `Status.json`, and
  waits for each update.
- **Sharing policies.** `FleetTests` checks one owned drive against two Anaconda fits and a Cutter fit
  under each policy, and that two identical slots in one fit still need two modules.
- **Spelling drift.** `ExplorationTests` predicts the same species whether a body's atmosphere is spelt
  the journal's way or Bioforge's.
- **The string tables.** `StringTableTests` and `UiTextTests` check the generated key constants against the
  English tables, every translation against the English for keys, placeholders and plural forms, and every
  translation of the game's names against `CatalogNameKeys.All`. A missing or misspelt entry fails here with
  its key.

## Conventions

Test methods use the `Method_Scenario_Expected` naming convention, so `.editorconfig` turns off CA1707 for
`*Tests.cs` files only. Fakes are hand-written in `Fakes.cs`; there is no mocking library.

`Katey.App.Tests` pins the UI culture to English and the formatting culture to invariant when the assembly
loads, so every sentence assert holds on any machine. `Katey.Ui.Tests` runs on xunit v3, which the headless
Avalonia package requires; the other suites run on xunit 2.
