# Katey.App

The framework-neutral half of the desktop front end: everything the window does that is not drawing. It
references [`Katey.Core`](../Katey.Core/README.md) and [`Katey.Journal`](../Katey.Journal/README.md) and
holds no dependency on any UI toolkit, which is what lets every behaviour here be tested as plain objects.

## The shape of the window

The window has three surfaces and a search box, and this project holds the state behind each:

- **The workbench** shows one page at a time. Every page describes one thing: an engineer, a module type,
  a blueprint, a material, a ship, a saved fit, a star system, or an index of those. Every mention of
  another thing on a page is a link that opens its page. The navigator keeps the history.
- **The live rail** follows the game: position, the next few route stops, the bodies worth a look in this
  system, what nearby covers a shortfall, and what the journal just said. It is one view model whether it
  is docked in the window or shown in a window of its own.
- **The plan drawer** opens over any page: the entries, modules to engineer and technology broker unlocks
  alike, the material totals against the live inventory, the commodities the unlocks need from a market,
  and the paste box for an EDSY export.
- **The search box** finds engineers, module types, blueprints, materials, systems you have visited, your
  ships and your saved fits, and opens the page for the chosen hit.
- **The navbar** sits beside the wordmark: one button per section, Engineers through Settings, lit while
  the page showing belongs to it, so an engineer's page lights Engineers and a blueprint lights Modules.
  The wordmark itself opens the start page, and back and forward sit between them.

Three mechanisms keep the pages consistent:

- **Sorting.** Every table has a `TableSort<T>`: its columns, the active one and the direction, and a
  `SortHeader` per column the view binds a heading to. Clicking the active heading flips the direction;
  clicking another sorts by it in the direction it was last in, so coming back to a column finds it as you
  left it. Every column's direction is remembered with the active one. A column of figures is declared
  `AlignRight`, and its heading sits at the column's right edge, over the figures.
- **Sections.** A page with several tables shows one at a time through a `SectionSwitch`: tabs the view
  binds to, and an `IsCurrent` per section that drives visibility.
- **Groups.** A page whose content rolls up by place on the ship, the modules index, builds one
  `ExpandableGroup` per place over a `GroupExpansion`, which remembers the open ones and does expand-all
  and collapse-all.
- **Preferences.** Sorts, sections, open groups, the materials toggles, the "Only my engineers" switch and
  the fleet filters are remembered through `UiPreferences`, a dictionary of dotted names that is saved with
  the state.
- **Reserves and trades.** `ReserveState` holds the material reserves and raises `Changed`; the workspace
  forwards that as a settings change so the shell saves, and `Workspace.Trades` runs the Core planner over
  the plan's needs, the live inventory and the reserves. The materials page and the drawer both show the
  result through `TradeView`. Editing a reserve on the materials page recomputes the trades alone, not the
  rows, so the box being typed in keeps focus; the binding updates when it loses focus.
  Breadcrumb links can also set a preference on arrival, which is how "Core internals" above a module type
  opens that group on the modules index.

Nothing leaves the machine unless its switch in `NetworkAccess` is on, and there is one switch per kind of
request. With the route switch on, the route page's plot section asks Spansh's galaxy plotter for a route
through `PlotState` and an `IRoutePlotter`. With the bodies switch on, `SystemBodiesState` asks an
`ISystemCatalog` about each system the ship arrives in. With the release switch on, `UpdateState` asks an
`IReleaseFeed` for Katey's latest release, once at startup and whenever the settings page asks, and compares
it with `AppVersion.Current`. The feed answers in the background, so the startup check does not delay the
window. `SpanshPlotter`, `SpanshSystemCatalog` and `GitHubReleaseFeed` are the network clients; the tests use
fakes. Every request carries `AppVersion.UserAgent`, which names Katey, its version and the project's address.

## File map

| Area | Types | Purpose |
| --- | --- | --- |
| `ShellViewModel.cs` | `ShellViewModel` | The window: applies the saved language, builds the workspace, the navigator, the search, the rail and the drawer; saves on every change; starts the journal watch. `SetLanguageCommand` switches the language live: the nav links are rebuilt, the current page is rebuilt from its key, the rail and the drawer refresh and the search reindexes. |
| `Workspace.cs` | `Workspace` | What pages, rail and search share: catalogs, trackers, plan, engineer selection, the engineer scope, saved fits, the plot, the material reserves and the trade plan, navigator, settings. `Route` and `System` take the position from the live route file's first stop when it is newer than the journal's last arrival, which is how a journal the game rewrote still places the ship. Raises one event per kind of change, on the UI thread. `UnlockedBecause` says why a technology broker unlock counts as bought: the journal records it, a module it unlocks was bought at outfitting, or one is fitted or stored. `AdvancePlan` moves the plan on by the engineering rolls reported since each entry last moved, through `PlanProgress`, and reports the entries that left. `Farness` says how far an owned module is from the ship, for the fit matcher to name the nearer of two equal modules. |
| `EngineerScope.cs` | `EngineerScope` | What the indexes and pickers may offer: everything, or only what the selected engineers can roll while the remembered "Only my engineers" switch is on. Narrows blueprints, grades, experimentals and module types, and gives the grade a picker starts at. |
| `EngineerSelection.cs` | `EngineerSelection` | The engineers the commander will visit. |
| `FleetState.cs` | `FleetState`, `EngineeringFilter` | The saved fits, the sharing policy, the location and engineering filters, and the match against the fleet. `Find` by name and hull is what tells a pasted build that replaces a fit from one that joins the list, `Update` replaces a fit in place, and `ProfileOf` reads a fit's jump numbers from the export text it keeps, once per fit until it changes. The unused list leaves out the fixtures every ship carries: the cargo hatch and the approach suite. |
| `JournalSinks.cs` | `JournalSinks` | The nine trackers, bundled so the monitor and the view models share one set. |
| `Backup/BackupState.cs` | `BackupState` | The journal backup: its folder, whether it runs on close, and the run in progress; raises on the UI thread. |
| `Trading/ReserveState.cs` | `ReserveState` | The material reserves the player set, keyed by journal symbol; saved with the state. |
| `Navigation/PageKey.cs` | `PageKind`, `PageKey` | Names a page: a kind and an entity id. |
| `Navigation/Link.cs` | `Link` | A labelled mention of a page with the command that opens it. |
| `Navigation/NavItem.cs` | `NavItem` | One navbar section: an index page, lit while the current page belongs to it. Its command opens the page, and goes back instead when that page is already open. |
| `Navigation/Navigator.cs` | `Navigator` | The current page, the history behind and ahead of it, and how far down each entry was scrolled as a fraction of its height, so back and forward return to the same place whatever the window size. |
| `UiPreferences.cs` | `UiPreferences` | Remembered view choices by dotted name; `Remove` forgets one. |
| `Text/Strings.resx`, `Text/Strings.<culture>.resx` | | The application's words, English in the neutral file and one file per shipped language, every entry with a comment saying where it shows and how long it may run. See [Words and languages](#words-and-languages). |
| `Text/Keys.cs` | `Keys` | One constant per key of `Strings.resx`, a plural group folded to its base key. Generated: `dotnet run tools/StringKeys.cs -- src/Katey.App/Text/Strings.resx src/Katey.App/Text/Keys.cs Katey.App.Text Keys`. |
| `Text/Strings.cs` | `Strings` | The localizer over `Strings.resx`: `Get`, `Format` and `Plural` by a key from `Keys`. |
| `Text/Languages.cs` | `Languages`, `LanguageOption` | The cultures Katey ships in, English first; the `app.language` preference; the picker's options, each language under its own name; `Apply`, which resolves the saved choice against the machine and makes it the language in force. |
| `Text/CatalogNames.resx`, `Text/CatalogNames.<culture>.resx` | | The game's own names per language: materials and their families, blueprints, effects, module types, module variant qualifiers, unlocks, mounts, hull armour, hulls, surface signal types, planet classes, atmosphere and volcanism words, lifetime figure labels and the words a passenger figure is composed from. The neutral file is empty by design: English is the data's. |
| `Text/CatalogNameKeys.cs` | `CatalogNameKeys` | How each game name is addressed in `CatalogNames.resx`, and `All`, the full key set a translation must hold. |
| `Text/TextSize.cs` | `TextSize`, `TextSizeOption` | The text size: the factors the picker offers as percentages of the design size, the `app.textScale` preference that remembers one, and `Read`, which takes the design size for anything but an offered factor. |
| `Text/Names.cs` | `Names` | Every game name in the language in force, falling back to the data: `Material`, `Blueprint`, `Experimental`, `ModuleType`, `Unlock`; `Module`, composed from the type's translated name, the class and rating and the mount, with a variant's translated qualifier words placed by the language's template and a named variant keeping its own name; `Hull`, the table's name for a hull before the journal's own localised name and `ShipNames`; `PlanetClass`, `Atmosphere` and `Volcanism`, which read the journal's phrase into its words and write it back through the language's template; `Figure` and `FigureSection` for the lifetime statistics, a passenger figure counted by type, class and outcome composed through the language's template. |
| `FleetChoices.cs` | `FleetChoices` | The pick-list options for the fleet's policy and filters, shared by the fleet and fit pages. |
| `FuelEstimate.cs` | `FuelEstimate`, `StopFuel` | The fuel picture for the route ahead: the game's fuel equation from the current ship's loadout figures, or its model fitted to recent jumps when the loadout lacks them, run from the live tank reading; and the tank against its capacity with what the next jump burns, as the fractions the fuel meter draws. |
| `Routing/IRoutePlotter.cs` | `IRoutePlotter`, `RoutePlotException` | What a plotter is: a request in, a route out, or a reason fit to show. |
| `Routing/SpanshPlotter.cs` | `SpanshPlotter` | Asks Spansh's galaxy plotter: one form post, then polls the job once a second. |
| `Routing/PlotState.cs` | `PlotState` | The held plot and the plot in progress, allowed by `NetworkAccess`; raises on the UI thread. |
| `Network/NetworkAccess.cs` | `NetworkAccess` | What may leave the machine: a switch per kind of request, off by default; saved. Everything that calls out consults it. |
| `Network/IReleaseFeed.cs` | `IReleaseFeed`, `Release`, `ReleaseFeedException`, `ReleaseLookupFailure` | Who knows Katey's latest release, the release as a version, its text and its page, and why a lookup gave no answer. |
| `Network/GitHubReleaseFeed.cs` | `GitHubReleaseFeed` | Asks GitHub's releases API for the latest release and reads its tag and page; runs the whole lookup on the thread pool. |
| `Network/UpdateState.cs` | `UpdateState` | The version running against the latest release, allowed by `NetworkAccess`: `CheckOnce` for startup and the switch turning on, `Check` for the button; words the result when read; raises on the UI thread. |
| `AppVersion.cs` | `AppVersion` | This build's version as text and as a number, the project's address, and the user agent every request carries. |
| `Exploration/ISystemCatalog.cs` | `ISystemCatalog`, `SystemCatalogException` | What a system catalogue is: a system id in, its bodies out, or a reason fit to show. |
| `Exploration/SpanshSystemCatalog.cs` | `SpanshSystemCatalog` | Asks Spansh for a system's dump and reads its bodies. |
| `Exploration/SystemBodiesState.cs` | `SystemBodiesState` | The catalogued bodies of the systems visited this run: one request per system on arrival when allowed, kept for the run, a failed lookup not retried for five minutes; raises on the UI thread. |
| `Routing/JumpProfiles.cs` | `JumpProfiles` | A ship's jump numbers from its journal loadout, through `JumpProfile.Build`. |
| `Mvvm/TableSort.cs` | `TableSort<T>`, `SortColumn<T>`, `SortHeader` | A table's sort state and the headings a view binds to; a column of figures is declared `AlignRight` and its heading says so. |
| `PreferenceKeys.cs` | `PreferenceKeys` | The preference keys Katey writes today; the saved preferences are pruned to them on load. |
| `Mvvm/SectionSwitch.cs` | `SectionSwitch`, `SectionItem` | A page's sections, one showing at a time. |
| `Mvvm/GroupExpansion.cs` | `GroupExpansion`, `ExpandableGroup` | A page's collapsible groups: which are open, remembered, with expand-all and collapse-all. |
| `Pages/Page.cs` | `Page` | The base: key, title, kicker, breadcrumb links, `Refresh`. |
| `Pages/IEdsyExportable.cs` | `IEdsyExportable` | A page whose subject copies to the clipboard as a journal `Loadout` for EDSY: ships and fits. |
| `Pages/PageFactory.cs` | `PageFactory` | Builds the page for a key, or a not-found page. |
| `Pages/HomePage.cs` | `HomePage`, `HomeStat`, `StatTone`, `HomeTile`, `NotFoundPage` | The start page: four figures that matter today, each with a tone, then the browse tiles, a tile's detail a link when it names a page, as the current system does; and the page for an id the data lacks. |
| `Pages/EngineersPage.cs` | `EngineersPage`, `EngineerListRow` | Every engineer with a search box, sortable columns and the visit tick, and the app-wide "Only my engineers" switch. |
| `Pages/EngineerPage.cs` | `EngineerPage`, `EngineerOfferGroup`, `EngineerOfferRow`, `EngineerPlanRow` | One engineer: distance, offers with add buttons, plan entries they can serve, stored modules and route stop. |
| `Pages/ModulesPage.cs` | `ModulesPage`, `ModuleGroupRow`, `ModuleTypeRow` | Every engineerable module type in one collapsible group per place, with expand-all and collapse-all; the engineers page's "Only my engineers" switch narrows the page to the types the selected engineers can roll a blueprint on. Open groups are remembered. |
| `Pages/ModuleTypePage.cs` | `ModuleTypePage`, `BlueprintRow`, `BlueprintEngineer`, `VariantRow`, `OwnedModuleRow`, `FitWantRow` | One module type: blueprints with grade pickers and add buttons, who offers them, experimentals, variants, owned modules, fits that want one. The pickers follow the engineer scope. |
| `Pages/BlueprintPage.cs` | `BlueprintPage`, `BlueprintGradeRow` | One blueprint across its module types: per-grade materials and engineers, and the bar that adds it to the plan on a chosen type, grade and experimental. |
| `Pages/MaterialsPage.cs` | `MaterialsPage`, `InventorySectionRow`, `InventoryMaterialRow`, `CategoryToggle` | Every material with have, need, short and its reserve to edit, the trades to make at the traders, a remembered switch per inventory, a name filter and a short-only filter. |
| `Pages/MaterialPage.cs` | `MaterialPage`, `MaterialSpendRow`, `MaterialUseRow`, `MaterialSourceRow` | One material: counts, where the plan spends it, what uses it, bodies here that carry it. |
| `Pages/UnlocksPage.cs` | `UnlocksPage`, `UnlockGroupRow`, `UnlockRow`, `UnlockState` | The technology broker unlocks, one broker at a time, in one collapsible group per category with the open groups remembered, each with its cost and whether what the plan leaves in the inventory covers it. Ticking a row adds a plan entry, unticking removes it; a bought unlock cannot be ticked. Below, the materials and commodities the planned unlocks need. |
| `Pages/FleetPage.cs` | `FleetPage`, `ShipListRow`, `FitListRow`, `UnusedModuleRow` | Ships, with prebuilt ones marked, saved fits with the paste box, and the unused modules under the policy and the location, place and engineering filters, one section at a time. A pasted build with a saved fit's name and hull replaces it on a second press. |
| `Pages/ShipPage.cs` | `ShipPage`, `FittedModuleRow`, `PrebuiltSummary`, `PrebuiltChangeRow` | One owned ship, its fitted modules each with a picker that plans engineering for it, a warning where that module came with a prebuilt ship, whether a prebuilt ship is still as redeemed, and the EDSY copy. |
| `Pages/FitPage.cs` | `FitPage`, `FitLineRow`, `DifferenceRow` | One saved fit: the comparison with a chosen ship, best match first, with engineer coverage, owned-elsewhere checks and a warning where the slot to engineer holds a prebuilt module, and a button that plans the gap; the report against everything owned; and the EDSY copy. |
| `Pages/SystemPage.cs` | `SystemPage` | The current system's bodies in full, or a visited system in brief. |
| `Pages/StatisticsPage.cs` | `StatisticsPage`, `StatRow`, `StatGroup`, `ChartSlice`, `ActivityToggle`, `SpeciesRow`, `PartnerRow`, `WingMissionRow` | Income by activity as a ring chart with switches and a period, the figures behind each activity, exobiology with and without the bonus, Powerplay, the wings and crews flown with, what was shared with each commander, the wing missions accepted and how each stands, and every lifetime figure, with zero figures hidden until a remembered switch shows them. The lifetime figures do not follow the period. |
| `Pages/RoutePage.cs` | `RoutePage`, `RouteStopRow`, `PlotStopRow`, `PlotProgressLine`, `StopStage`, `VisitState` | Two sections: the game's route with the stage first, the distance from the start and the fuel on arrival per stop, and the stop rows the rail windows, with `RerouteLine` wording a target off the route file as the game's re-plot through it and `RerouteJumps` giving the game's own count while the file lags; and the Spansh plot with the form that asks for it, the waypoints and the ship's progress along them. `PlotTarget` is one entry of the form's picker, a ship or a saved fit with its range, the ship being flown flagged so the view can colour it; the picker lists the chosen target, the ship being flown, then the chosen hull's fits, the ships and the other fits, narrowed by `PlotFilter`. The form's destination, routing and options are `UiPreferences` entries written as they change; the chosen target and the cargo are entries too, removed when the plot is forgotten, so they hold across pages and restarts while a plot stands and the form then opens on the ship being flown with what it carries. |
| `Pages/SettingsPage.cs` | `SettingsPage` | The journal folder and the watch, the backup folder, switch and button, the saved data folder, the outgoing connection switches, and the release check: the version running, its status line, the Check now command and the release page for the view to open. Setting the saved data folder moves the saved data at once. |
| `Rail/RailViewModel.cs` | `RailViewModel`, `RailStopRow`, `RailBodyRow` | The live rail, showing the route while the drive is charging in supercruise, a jump has begun from normal space or the ship is in hyperspace, and the bodies otherwise, from arrival on, with the route kept to one line above them; what is targeted does not count, since the game targets the next jump by itself on arrival. Bodies nearest first or, by a remembered switch, biology first; the remembered overlay, click-through and window-place settings its own window follows. |
| `Search/SearchViewModel.cs` | `SearchViewModel`, `SearchResult` | The search box over a catalog index and a journal index. |
| `ViewModels/PlanDrawerViewModel.cs` | `PlanDrawerViewModel` | The drawer: open state, badge, import form, totals, and the trades to make at the traders. |
| `ViewModels/PlanViewModel.cs` | `PlanViewModel` | The entry list, with two-click clearing; `Contains` says whether a page's candidate entry repeats work already planned, by `PlanEntry.IsSameWorkAs`; `Update` brings it in line with a plan the progress rule produced, and `Load` anchors an entry saved without a `CraftsAfter`. |
| `ViewModels/PlanEntryViewModel.cs` | `PlanEntryViewModel`, `RollViewModel` | One editable entry: enable switch, grade, experimental, rolls, engineers, warning, materials. An unlock entry has only the switch, its materials and its commodities. |
| `ViewModels/TotalsViewModel.cs` | `TotalsViewModel`, `TotalsSection` | The shopping list with have and short per material, and the commodities to buy at a market. |
| `ViewModels/ImportViewModel.cs` | `ImportViewModel`, `ImportedModuleViewModel` | Paste, read, tick, add. |
| `ViewModels/BodyRows.cs` | `BodyRowBuilder`, `BodyRow`, `GenusRow` | Scanned bodies ready to print, star by star with each star's class and scoopability in words, shared by the system page and the rail. |
| `ViewModels/CarriedDataSummary.cs` | `CarriedDataSummary` | What the unsold data aboard is worth: the cartographics estimate per scanned body, the species table per sample with the first-logged bonus where the journal says nobody had logged it; shown on the home page and the stats page. |
| `ViewModels/SystemValue.cs` | `SystemValue` | The current system's worth: cartographics and exobiology, each as scanned and with its first bonus where it likely applies, what of it is still aboard and what has sold; as the system page's two sentences and the rail's unsold line. |
| `ViewModels/SystemSales.cs` | `SystemSales` | What one system's data has sold for: a sale of several systems shared by Katey's estimate of their bodies, a species sale credited to the system its sample came from. |
| `ViewModels/ScanProgress.cs` | `ScanProgress` | The current system's scan progress in words, as a figure and as a share, from the snapshot's `BodiesKnown`, shared by the home page, the system page and the rail. |
| `ViewModels/Display.cs` | `Choice`, `MaterialChip`, `MaterialRow`, `Display` | Pick-list options, display records and shared formatting, every word from the table: group, category and region labels, credits, distances, durations and grades through keyed unit templates, `Plural` over a plural base key, `List` and `Names` joined with the language's separator, `Upper` for a label set in capitals. |
| `ViewModels/ShoppingListText.cs` | `ShoppingListText` | The plan's totals as plain text for the clipboard, in the language in force. |
| `ViewModels/StarText.cs` | `StarText` | A star in words from what `StarClasses` reads off its class: `Describe`, `Brief` and `Label`, with the giant words. |
| `ViewModels/Place.cs` | `Place` | Somewhere in the galaxy as a system link plus a detail, so every system name on a page opens its page. |
| `ViewModels/Phrase.cs` | `PhrasePart`, `LinkTone`, `Phrase` | A sentence that carries links in its running text, so a system or an engineer named mid-sentence opens its page; a link has a tone: accent, the HUD blue, ink or dim. `Phrase.Format` fills a keyed template whose placeholders may be links, engineer links or other sentences, so a translation can put the links in any order; `Phrase.Of` builds one from parts in order, `Plain` reads the words alone. |
| `ViewModels/EngineerLink.cs` | `EngineerLink` | An engineer's name as Katey shows it everywhere, made by `Workspace.EngineerLinkFor`: their page, whether you ticked them and whether the engineers switch narrows the lists, from which the name is highlighted, dimmed or plain; `Names` lists several in a sentence. |
| `ViewModels/TradeRows.cs` | `TradeView`, `TraderGroupRow`, `TradeRow`, `UnmetRow` | A trade plan laid out for a page: a headline, one group per trader with its trades and what stays short, each material a link to the materials page landing on it, each trade naming the two materials' in-game families, and a note on which targets are reserves. Shared by the materials page and the drawer. |
| `Persistence/StateStore.cs` | `PersistedState`, `IStateStore`, `JsonFileStateStore` | The saved plan, engineer selection, fits, sharing policy, journal folder, bacteria filter, rail placement, view preferences, outgoing connection switches, last plot, backup folder and switch, and material reserves; where it lives, and moving it. |
| `Mvvm/` | `ObservableObject`, `RelayCommand` | The two binding primitives the view models need. |
| `Log.cs` | `Log` | Source-generated log messages. |
| `Logging/FileLoggerProvider.cs` | `FileLoggerProvider` | A logger provider that appends to one text file above a floor, rolling the file over past a size, passing every line through a scrubber, and writing a crash at Critical. |
| `Logging/LogArguments.cs` | `LogArguments` | Reads `--log-level <level>` and `--verbose` from the command line: the log file's floor, Information when absent. |
| `Logging/LogScrubber.cs` | `LogScrubber` | Strips the folders from every path in a log line, keeping the file name, and replaces the account and machine names, before the line is written. |

## Words and languages

Every word a page, the rail, the drawer or the search shows comes from a table by key; no view model holds a
literal. The invariant mechanism is [`Katey.Localization`](../Katey.Localization/README.md); this project
holds the words.

- **Keys.** `Strings.resx` is keyed `Area.Thing`: `Rail.NextJump`, `Column.Grade`, `Unit.Credits`. A counted
  text is a plural group, `Plural.Jumps.one` and `Plural.Jumps.other`, used as `Display.Plural(Keys.Plural_Jumps, n)`.
  `Keys.cs` carries one constant per key, so a misspelt key fails to compile; the drift test fails when the
  constants and the table disagree, which is the cue to run the generator named in the file map.
- **Templates.** A text with placeholders is filled by name, as `Strings.Format(Keys.Route_JumpOf, ...)` does
  with an `("index", i)` pair per placeholder.
  A sentence that names a system or an engineer goes through `Phrase.Format` with the `Link` as the value, and
  `PhraseBlock` shows it. Fragments are never concatenated: when a sentence has two shapes, it has two keys.
- **Figures.** Numbers and dates inside a template take the formatting culture, the machine's regional setting.
  Keys, ids, URLs, journal text and preference values stay invariant.
- **Game names.** `Names` wraps every catalog name, hull and armour name, journal body descriptor and
  lifetime figure label. Its table's neutral file is empty, so English is always the data's own word and a
  language with no entry shows the same. The completeness test checks every shipped language against
  `CatalogNameKeys.All`. A hull's name comes from the table before the journal's own localised name, so a hull
  reads in Katey's language rather than the game client's. The four Latin-script languages hold the game's own
  names, which their clients do not translate; the Russian table holds Cyrillic transliterations.
- **The switch.** `Languages.Apply` runs in the shell's constructor before any page is built, from the
  `app.language` preference or the machine's culture, so the first page is already in the right language.
  `ShellViewModel.SetLanguageCommand` saves the choice, calls `Language.Set`, rebuilds the nav links, rebuilds
  the current page from its key through `Navigator.Rebuild`, refreshes the rail, words the plan's rows again
  through `PlanViewModel.Reword`, updates the drawer and reindexes the search. State that outlives a page,
  such as a backup, plot or lookup status, keeps facts and words them when read, so it follows the switch
  too. `Languages.Supported` lists the cultures that ship: English, German, Spanish, French, Portuguese
  (Brazil), Russian.
- **Adding a key.** Add the English entry to `Strings.resx` with a comment, regenerate `Keys.cs`, use the key,
  then add the entry to every `Strings.<culture>.resx`; the translation test lists what is missing.
- **Adding a language.** Add `Strings.<culture>.resx`, `CatalogNames.<culture>.resx` here and
  `UiStrings.<culture>.resx` in `Katey.Ui`, add the culture to `Languages.Supported`, and add it to the
  `SatelliteResourceLanguages` list in `publish.bat` and `publish.sh`. If the language's plural forms are not
  one and other, teach `PluralRules` first.
- **Tests.** `tests/Katey.App.Tests` pins the UI culture to English and the formatting culture to invariant, so
  the sentence asserts hold on any machine. `StringTableTests` checks the keys, the translations and the names.

## How the pieces connect

`ShellViewModel` owns everything. It reads the saved state, builds the `Workspace` over one
`JournalSinks` and one `IJournalMonitor`, attaches a `Navigator`, opens the start page, and wires the
events:

- `PlanViewModel.Changed` updates the drawer, refreshes the current page and the rail, and saves.
- `EngineerSelection.Changed` refreshes every entry's engineers and warning, the drawer and the current
  page, and saves.
- `FleetState.Changed` refreshes the current page and the search index, and saves. `FilterChanged`
  refreshes the current page only; filters are not saved.
- `Workspace.LiveChanged`, raised after every journal update, first moves the plan on by any engineering
  the game reported, which saves through `PlanViewModel.Changed` when something moved, then updates the
  drawer, the current page, the rail and the search index.
- `Workspace.WatchChanged` refreshes the current page and the rail when the monitor starts, stops or
  finishes the replay.
- `Workspace.SettingsChanged` and a change to the rail's placement save.
- `PlotState.Changed`, raised when the switch, the plot or its status changes, refreshes the current page
  and the rail, and saves.
- `BackupState.Changed`, raised when the folder, the switch or a run's status changes, does the same.

The shell's `Shutdown` runs the journal backup when it is set to run on close. The window calls it as the
app exits, before the monitor is disposed.

Pages are cheap to build. The navigator builds a fresh one on every `Open` and calls `Refresh` on the
current one when any of the events above fires, so a page always reads the latest trackers without
subscribing to anything itself.

While the monitor is replaying the history, every snapshot the workspace hands out reads as empty and
`IsCatchingUp` is true, so no page or rail section shows a position, a system, a fleet or a figure from
partway through the history. The shell and the rail show the replay's progress instead, and everything
appears together when the replay finishes.

## Distances

The engineers table carries each engineer's system coordinates, which is how pages sort engineers by
distance from the ship. When the player has visited an engineer's system, `Workspace.PositionOf` uses
the coordinates the journal recorded for it instead of the table's.

## Threading

The journal monitor raises its events on a background thread. `Workspace` takes a dispatcher delegate,
`Action<Action>`, and runs every monitor event through it, so every event it raises and every bound
property changes on the UI thread. The window passes Avalonia's dispatcher; the tests pass a delegate
that runs the action immediately. `PlotState` and `BackupState` run a plot or a backup on a thread-pool
thread and hand the result back through the same dispatcher; the backup on close runs on the calling
thread, so it finishes before the process ends.

## Saved state

`JsonFileStateStore` keeps one JSON file at `Katey/state.json` under the user's local application data folder.
It holds:

- the plan entries and the selected engineers, by the game's engineer id;
- the saved fits and the sharing policy;
- the journal folder, the backup folder and whether the backup runs on close;
- whether the rail was in its own window, and the view preferences;
- whether Katey may ask Spansh for routes and for system bodies, whether it may ask GitHub for its latest
  release, and the last plot.

Read failures log a warning and start the app empty; write failures log an error and leave the app running. On
load, entries whose blueprint or experimental the catalog no longer knows are dropped, and a file from before
a setting existed loads with that setting's default.

The user can keep the saved data in a folder of their own. `JsonFileStateStore.Relocate` points the store at
`katey-state.json` in that folder and writes the full path to a pointer file, `Katey/location.txt` beside the
default state file, which `Configured` reads on the next start. The shell's move saves the current data to the
old place first, then relocates. When the folder already holds a saved state, the shell loads it and applies
every part of it to the running workspace through the `Replace` and `Restore` methods on the selection, the
fleet, the preferences and the plot, and rebuilds the current page. When it holds none, the current data is
written there. Nothing is deleted at the old place.
