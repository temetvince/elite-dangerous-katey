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
  `SortHeader` per column the view binds a heading to. Clicking the active heading flips the direction.
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

One thing leaves the machine, and only on request. With the Spansh switch on in settings, the route page's
plot section asks Spansh's galaxy plotter for a route through `PlotState` and an `IRoutePlotter`.
`SpanshPlotter` is the only network client in the app; the tests use a fake.

## File map

| Area | Types | Purpose |
| --- | --- | --- |
| `ShellViewModel.cs` | `ShellViewModel` | The window: builds the workspace, the navigator, the search, the rail and the drawer; saves on every change; starts the journal watch. |
| `Workspace.cs` | `Workspace` | What pages, rail and search share: catalogs, trackers, plan, engineer selection, the engineer scope, saved fits, the plot, the material reserves and the trade plan, navigator, settings. `Route` and `System` take the position from the live route file's first stop when it is newer than the journal's last arrival, which is how a journal the game rewrote still places the ship. Raises one event per kind of change, on the UI thread. `UnlockedBecause` says why a technology broker unlock counts as bought: the journal records it, a module it unlocks was bought at outfitting, or one is fitted or stored. `AdvancePlan` moves the plan on by the engineering rolls reported since each entry last moved, through `PlanProgress`, and reports the entries that left. `Farness` says how far an owned module is from the ship, for the fit matcher to name the nearer of two equal modules. |
| `EngineerScope.cs` | `EngineerScope` | What the indexes and pickers may offer: everything, or only what the selected engineers can roll while the remembered "Only my engineers" switch is on. Narrows blueprints, grades, experimentals and module types, and gives the grade a picker starts at. |
| `EngineerSelection.cs` | `EngineerSelection` | The engineers the commander will visit. |
| `FleetState.cs` | `FleetState`, `EngineeringFilter` | The saved fits, the sharing policy, the location and engineering filters, and the match against the fleet. `Find` by name and hull is what tells a pasted build that replaces a fit from one that joins the list, `Update` replaces a fit in place, and `ProfileOf` reads a fit's jump numbers from the export text it keeps, once per fit until it changes. The unused list leaves out the fixtures every ship carries: the cargo hatch and the approach suite. |
| `JournalSinks.cs` | `JournalSinks` | The nine trackers, bundled so the monitor and the view models share one set. |
| `Backup/BackupState.cs` | `BackupState` | The journal backup: its folder, whether it runs on close, and the run in progress; raises on the UI thread. |
| `Trading/ReserveState.cs` | `ReserveState` | The material reserves the player set, keyed by journal symbol; saved with the state. |
| `Navigation/PageKey.cs` | `PageKind`, `PageKey` | Names a page: a kind and an entity id. |
| `Navigation/Link.cs` | `Link` | A labelled mention of a page with the command that opens it. |
| `Navigation/NavItem.cs` | `NavItem` | One navbar section: an index page, lit while the current page belongs to it. |
| `Navigation/Navigator.cs` | `Navigator` | The current page, the history behind and ahead of it, and how far down each entry was scrolled as a fraction of its height, so back and forward return to the same place whatever the window size. |
| `UiPreferences.cs` | `UiPreferences` | Remembered view choices by dotted name; `Remove` forgets one. |
| `FleetChoices.cs` | `FleetChoices` | The pick-list options for the fleet's policy and filters, shared by the fleet and fit pages. |
| `FuelEstimate.cs` | `FuelEstimate`, `StopFuel` | The fuel picture for the route ahead: the game's fuel equation from the current ship's loadout figures, or its model fitted to recent jumps when the loadout lacks them, run from the live tank reading; and the tank against its capacity with what the next jump burns, as the fractions the fuel meter draws. |
| `Routing/IRoutePlotter.cs` | `IRoutePlotter`, `RoutePlotException` | What a plotter is: a request in, a route out, or a reason fit to show. |
| `Routing/SpanshPlotter.cs` | `SpanshPlotter` | Asks Spansh's galaxy plotter: one form post, then polls the job once a second. Every request carries a user agent naming Katey, its version and the project's address. |
| `Routing/PlotState.cs` | `PlotState` | The held plot and the plot in progress, allowed by `NetworkAccess`; raises on the UI thread. |
| `Network/NetworkAccess.cs` | `NetworkAccess` | What may leave the machine: a switch per kind of request, off by default; saved. Everything that calls out consults it. |
| `Exploration/ISystemCatalog.cs` | `ISystemCatalog`, `SystemCatalogException` | What a system catalogue is: a system id in, its bodies out, or a reason fit to show. |
| `Exploration/SpanshSystemCatalog.cs` | `SpanshSystemCatalog` | Asks Spansh for a system's dump and reads its bodies. |
| `Exploration/SystemBodiesState.cs` | `SystemBodiesState` | The catalogued bodies of the systems visited this run: one request per system on arrival when allowed, kept for the run, a failed lookup not retried for five minutes; raises on the UI thread. |
| `Routing/JumpProfiles.cs` | `JumpProfiles` | A ship's jump numbers from its journal loadout, through `JumpProfile.Build`. |
| `Mvvm/TableSort.cs` | `TableSort<T>`, `SortColumn<T>`, `SortHeader` | A table's sort state and the headings a view binds to. |
| `Mvvm/SectionSwitch.cs` | `SectionSwitch`, `SectionItem` | A page's sections, one showing at a time. |
| `Mvvm/GroupExpansion.cs` | `GroupExpansion`, `ExpandableGroup` | A page's collapsible groups: which are open, remembered, with expand-all and collapse-all. |
| `Pages/Page.cs` | `Page` | The base: key, title, kicker, breadcrumb links, `Refresh`. |
| `Pages/IEdsyExportable.cs` | `IEdsyExportable` | A page whose subject copies to the clipboard as a journal `Loadout` for EDSY: ships and fits. |
| `Pages/PageFactory.cs` | `PageFactory` | Builds the page for a key, or a not-found page. |
| `Pages/HomePage.cs` | `HomePage`, `HomeStat`, `StatTone`, `HomeTile`, `NotFoundPage` | The start page: four figures that matter today, each with a tone, then the browse tiles; and the page for an id the data lacks. |
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
| `Pages/RoutePage.cs` | `RoutePage`, `RouteStopRow`, `PlotStopRow`, `PlotProgressLine`, `StopStage`, `VisitState` | Two sections: the game's route with the stage first, the distance from the start and the fuel on arrival per stop, and the stop rows the rail windows; and the Spansh plot with the form that asks for it, the waypoints and the ship's progress along them. `PlotTarget` is one entry of the form's picker, a ship or a saved fit with its range, the ship being flown flagged so the view can colour it; the picker lists the chosen target, the ship being flown, then the chosen hull's fits, the ships and the other fits, narrowed by `PlotFilter`. The form's destination, routing and options are `UiPreferences` entries written as they change; the chosen target and the cargo are entries too, removed when the plot is forgotten, so they hold across pages and restarts while a plot stands and the form then opens on the ship being flown with what it carries. |
| `Pages/SettingsPage.cs` | `SettingsPage` | The journal folder and the watch, the backup folder, switch and button, the saved data folder, and the Spansh switch. Setting the saved data folder moves the saved data at once. |
| `Rail/RailViewModel.cs` | `RailViewModel`, `RailStopRow`, `RailBodyRow` | The live rail, showing the route while another system is the destination in `Status.json`, the drive is charging or the ship is in hyperspace, and the bodies otherwise, with the bodies nearest first or, by a remembered switch, biology first, and the remembered overlay, click-through and window-place settings its own window follows. |
| `Search/SearchViewModel.cs` | `SearchViewModel`, `SearchResult` | The search box over a catalog index and a journal index. |
| `ViewModels/PlanDrawerViewModel.cs` | `PlanDrawerViewModel` | The drawer: open state, badge, import form, totals, and the trades to make at the traders. |
| `ViewModels/PlanViewModel.cs` | `PlanViewModel` | The entry list, with two-click clearing; `Contains` says whether a page's candidate entry repeats work already planned, by `PlanEntry.IsSameWorkAs`; `Update` brings it in line with a plan the progress rule produced, and `Load` anchors an entry saved without a `CraftsAfter`. |
| `ViewModels/PlanEntryViewModel.cs` | `PlanEntryViewModel`, `RollViewModel` | One editable entry: enable switch, grade, experimental, rolls, engineers, warning, materials. An unlock entry has only the switch, its materials and its commodities. |
| `ViewModels/TotalsViewModel.cs` | `TotalsViewModel`, `TotalsSection` | The shopping list with have and short per material, and the commodities to buy at a market. |
| `ViewModels/ImportViewModel.cs` | `ImportViewModel`, `ImportedModuleViewModel` | Paste, read, tick, add. |
| `ViewModels/BodyRows.cs` | `BodyRowBuilder`, `BodyRow`, `GenusRow` | Scanned bodies ready to print, star by star with each star's class and scoopability in words, shared by the system page and the rail. |
| `ViewModels/CarriedDataSummary.cs` | `CarriedDataSummary` | What the unsold data aboard is worth: the cartographics estimate per scanned body, the species table per sample with the first-logged bonus where the journal says nobody had logged it; shown on the home page and the stats page. |
| `ViewModels/ScanProgress.cs` | `ScanProgress` | The current system's scan progress in words, as a figure and as a share, from the snapshot's `BodiesKnown`, shared by the home page, the system page and the rail. |
| `ViewModels/Display.cs` | `Choice`, `MaterialChip`, `MaterialRow`, `Display` | Pick-list options, display records and shared formatting. |
| `ViewModels/Place.cs` | `Place` | Somewhere in the galaxy as a system link plus a detail, so every system name on a page opens its page. |
| `ViewModels/TradeRows.cs` | `TradeView`, `TraderGroupRow`, `TradeRow`, `UnmetRow` | A trade plan laid out for a page: a headline, one group per trader with its trades and what stays short, each material a link to the materials page landing on it, each trade naming the two materials' in-game families, and a note on which targets are reserves. Shared by the materials page and the drawer. |
| `Persistence/StateStore.cs` | `PersistedState`, `IStateStore`, `JsonFileStateStore` | The saved plan, engineer selection, fits, sharing policy, journal folder, bacteria filter, rail placement, view preferences, Spansh switch, last plot, backup folder and switch, and material reserves; where it lives, and moving it. |
| `Mvvm/` | `ObservableObject`, `RelayCommand` | The two binding primitives the view models need. |
| `Log.cs` | `Log` | Source-generated log messages. |

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

`JsonFileStateStore` keeps one JSON file at `Katey/state.json` under the user's local application data
folder. It holds the plan entries, the selected engineer names, the saved fits, the sharing policy, the
journal folder, whether the rail hides bacteria-only bodies, whether the rail was in its own window, the view
preferences, whether Katey may ask Spansh for routes, the last plot, the backup folder and whether the
backup runs on close.
Read failures log a warning and start the app empty; write failures log an error and leave the app running.
On load, entries whose blueprint or experimental the catalog no longer knows are dropped, and a file from
before a setting existed loads with that setting's default.

The user can keep the saved data in a folder of their own. `JsonFileStateStore.Relocate` points the store
at `katey-state.json` in that folder and writes the full path to a pointer file, `Katey/location.txt`
beside the default state file, which `Configured` reads on the next start. The shell's move saves the
current data to the old place first, then relocates; when the folder already holds a saved state the
shell loads it and applies every part of it to the running workspace through the `Replace` and `Restore`
methods on the selection, the fleet, the preferences and the plot, and rebuilds the current page. When it
holds none, the current data is written there. Nothing is deleted at the old place.
