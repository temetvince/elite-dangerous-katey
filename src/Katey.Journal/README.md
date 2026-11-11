# Katey.Journal

Reads Elite Dangerous player journal files and keeps eight trackers current from them, and backs the folder
up. It knows nothing
about engineering and does not reference `Katey.Core`; the two meet in `Katey.App`.

## File map

| File | Types | Purpose |
| --- | --- | --- |
| `JournalFolder.cs` | `JournalFolder` | Where the game writes journals on this machine, and the journal files in a folder, oldest first. |
| `JournalEvent.cs` | `JournalEvent` | One journal line: timestamp, event name, and the line as a detached JSON element. |
| `JsonRead.cs` | `JsonRead` | Tolerant readers for the loosely typed fields journal documents carry, including item symbols in canonical form. |
| `IJournalSink.cs` | `IJournalSink` | What a tracker is: a set of wanted events, `Reset`, `Apply`. |
| `IJournalMonitor.cs` | `IJournalMonitor`, `MonitorState`, `CatchUpProgress` | The contract a front end drives. |
| `JournalMonitor.cs` | `JournalMonitor` | Replays history into the sinks, then polls the newest journal and the live files; a journal that shrinks is read again from its start. |
| `LiveFiles.cs` | `NavRoute`, `RouteStop`, `ShipStatus` | `NavRoute.json` and `Status.json`, parsed: the route's stops, and the ship's flags read as docked, landed, in supercruise, charging, in hyperspace and scooping, its body, destination, main-tank fuel and cargo. |
| `MaterialInventory.cs` | `MaterialInventory`, `MaterialInventorySnapshot` | Material counts by journal symbol. |
| `RouteTracker.cs` | `RouteTracker`, `RouteProgress`, `VisitedSystem`, `StarPosition`, `JumpRecord` | Current system and its coordinates, next jump target, every system ever visited with its coordinates and last visit, and the last twelve jumps with their distance, fuel and boost. |
| `ExplorationTracker.cs` | `ExplorationTracker`, `SystemSnapshot`, `ScannedBody`, `OrganicScan`, `LearnedSpeciesValue` | The current system's bodies, landings across history, and what each species has sold for. |
| `FleetTracker.cs` | `FleetTracker`, `FleetSnapshot`, `OwnedShip`, `StoredModule`, `FittedModule`, `JournalEngineering` | Ships, their last-known fits with the modules that came with a prebuilt ship marked, and stored modules. |
| `ActivityTracker.cs` | `ActivityTracker`, `ActivityItem`, `ActivityKind` | The last sixty things worth telling the player about, newest first. |
| `StatisticsTracker.cs` | `StatisticsTracker`, `StatisticsSnapshot`, `LifetimeSection`, `IncomeEntry`, `IncomeKind`, `SharedEntry`, `WingMission`, `MissionOutcome`, `PowerplayStanding`, `PowerplayEntry`, `PowerplayAction` | The game's lifetime figures, every credit of income by activity, the income the game marks as shared with other players, every wing mission accepted and how it stands, and the Powerplay standing with merits and deliveries. |
| `CarriedDataTracker.cs` | `CarriedDataTracker`, `CarriedData`, `CarriedSystem`, `CarriedBody`, `CarriedSample`, `SoldData`, `ExplorationSale`, `OrganicSale` | Every scanned body until its system is sold and every completed organic sample until sold, all lost on death; and a ledger of every sale with what the game paid. |
| `UnlockTracker.cs` | `UnlockTracker`, `UnlocksSnapshot`, `UnlockRecord`, `UnlockEvidence` | Every technology broker purchase, with the broker and the item symbols it made available, and every module bought at outfitting, which proves the item was unlocked. |
| `CraftTracker.cs` | `CraftTracker`, `CraftsSnapshot`, `CraftRecord` | Every engineering roll: the module, its slot, the blueprint, the grade, how far the grade got and the experimental effect on the module afterwards. |
| `CompanyLog.cs` | `CompanyLog`, `Company`, `CompanySession` | Whether the player is in a wing or a crew at each point of the history, with whom, and every such session. |
| `JournalBackup.cs` | `IJournalBackup`, `JournalBackup`, `BackupResult` | Mirrors the journal folder into another, copying only what is new or changed. |
| `Log.cs` | `Log` | Source-generated log messages. |

## Where the journal lives

`JournalFolder.Candidates()` lists, most likely first:

1. `<user profile>/Saved Games/Frontier Developments/Elite Dangerous`, the Windows location.
2. The Proton prefix inside each known Steam library on Linux:
   `<steam>/steamapps/compatdata/359320/pfx/drive_c/users/steamuser/Saved Games/Frontier Developments/Elite Dangerous`,
   for `~/.steam/steam`, `~/.local/share/Steam` and the Flatpak Steam library.

`FindDefault()` returns the first candidate that exists. `JournalFiles` orders the files by the start time
in each name, which `StartedAt` reads from either format the game has used. Write times are not trusted:
a cloud sync or a restore can scramble them, and a history replayed out of order loses state, such as which
ship was redeemed from a bundle. A file whose name is in neither format sorts by its write time.

## How the monitor follows the game

`JournalMonitor.Start(folder)`:

1. Resets every sink.
2. Replays every journal file, oldest first, reporting `Progress` after each. A line is parsed only when
   some sink's `InterestedEvents` names its event; the event name is read with a plain string search
   first, so a multi-year history takes seconds.
3. Reads `NavRoute.json` and `Status.json`.
4. Polls on a fixed interval, one second by default: new bytes in the newest journal, a newer journal
   file, and a changed write time on either live file. `Updated` is raised at most once per poll, after
   everything changed in it has been applied.

Partial trailing lines wait for the next poll. If the folder does not exist, `State` is `FolderMissing`
and nothing is read. `Stop` ends the loop and leaves the sinks as they are. Polling is deliberate: the
game appends without signalling, so a file-system notification would still have to be followed by a read.

Sinks are called on the monitor's thread, one call at a time. Each tracker locks internally and hands out
immutable snapshots, so a UI thread can read them at any time. The monitor's events are raised on its
thread; a front end must marshal them.

## What each tracker reads

### MaterialInventory

| Event | Effect |
| --- | --- |
| `Materials` | Replaces the whole inventory. Written every time a commander loads. Until one has been seen, nothing else is applied. |
| `MaterialCollected` | Adds `Count` of `Name`. |
| `MaterialDiscarded`, `ScientificResearch` | Removes `Count` of `Name`. |
| `MaterialTrade` | Removes `Paid.Quantity` of `Paid.Material`, adds `Received.Quantity` of `Received.Material`. |
| `EngineerCraft` | Removes each of `Ingredients`. |
| `Synthesis`, `TechnologyBroker` | Removes each of `Materials`. |
| `MissionCompleted` | Adds each of `MaterialsReward`. |
| `EngineerContribution` | Removes `Quantity` of `Material` when `Type` is `Materials`. |
| `Commander`, `LoadGame` | Records the commander's name. |

Counts never go below zero, a material at zero is dropped, and symbols are compared case-insensitively.

### RouteTracker

`Location`, `FSDJump` and `CarrierJump` set the current system and its `StarPos` coordinates. They also record
the arrival in the known-systems map, which covers the whole history: name, id, coordinates, last visit, visit
count, and whether the system was ever surveyed, which `FSSDiscoveryScan`, `NavBeaconScan` and
`FSSAllBodiesFound` set. The route itself comes from the monitor's `Route`. The other events:

- `FSDJump` also records the jump's `JumpDist`, `FuelUsed` and `BoostUsed` in a list of the last twelve jumps.
  `LoadGame`, `ShipyardSwap` and `ShipyardNew` clear the list, so it only ever describes the ship being flown.
  A `Loadout` whose frame shift drive differs from the last one seen, by item or by engineering, clears it
  too, so a refitted drive is not judged by the old drive's jumps.
- `FSDTarget` gives the next destination, its star class and the jumps remaining on the route.
- `StartJump` with `JumpType` `Hyperspace` marks the ship as mid-jump until arrival and makes the jump's own
  destination the target, whatever was targeted before.
- `NavRouteClear` forgets the target, except mid-jump: the game clears the route during the last jump of a
  route, and the destination of the jump under way stands until arrival.

`JetConeBoost` sets `Supercharged`, and the next jump clears it, so a plot asked for while the drive holds a
boost can say so.

### ExplorationTracker

Entering a system switches to that system's body list. A system's bodies, honk and body count are kept when
the player leaves and come back when the player returns, so a body scanned on an earlier visit within the
journal history is listed again with the time it was scanned. The events:

- `FSSDiscoveryScan` gives the body count and, through its progress figure, whether every body was already in
  the player's data when the honk fired. A progress of 1 means the system was scanned on an earlier visit, and
  the game writes no `Scan` for those bodies again.
- `BodiesKnown` on the snapshot is the whole count after such a honk, as it is once `FSSAllBodiesFound`
  arrives. Otherwise it is the stars and planets scanned this visit, `BodiesScanned`, since a partial progress
  figure is weighted rather than a count. Belt clusters are recorded but are not in the game's body count, so
  neither figure counts them.
- `Scan` records a body: star type or planet class, distance, landable, atmosphere, volcanism, gravity
  converted to g, temperature, pressure, mass, terraform state, rings, surface materials, and the
  `WasDiscovered`, `WasMapped` and `WasFootfalled` flags. The footfall flag reads false in a populated system,
  where footfall cannot be claimed, so the snapshot's `Populated`, from the arrival event's population, says
  when it means nothing.
- `FSSBodySignals` and `SAASignalsFound` add biological and geological counts and the genus list; counts only
  ever rise. Any other signal type is kept as a `BodySignal`: the game's symbol with the client's words
  beside it, so the application can name it in its own language.
- `SAAScanComplete` marks a body mapped by the player.
- `ApproachBody`, `Touchdown`, `Liftoff` and `LeaveBody` track the current body. Every `Touchdown` across
  history is remembered, so a body shows as landed-on when you return.
- `ScanOrganic` records species and sampling stage per body, and `WasLogged`: whether someone had logged the
  species there before the player sampled it. The sampler holds one species at a time, so starting a
  different species before the third sample forgets the partial one. Stages are kept with the system's
  bodies, so they come back on a return visit and stand after a sale.
- `SellOrganicData` records, across history, the value and first-logged bonus each species sold for, and marks
  every species sampled in full as sold, which the snapshot reports per species and per body.
- `SellExplorationData` and `MultiSellExplorationData` mark every scanned body in the systems they name as
  sold, which the snapshot reports per body.

A first, once the player's, stays theirs. After a sale the game's flags report the player's own record, so a
later `Scan` of a remembered body keeps three things, whatever the new flags say:

- undiscovered, when the earlier scan said so and the system has been sold since;
- unmapped, when the player mapped it and the system has been sold since;
- unfootfalled, when the player has landed on it.

A body never sold takes the new flags as they come: a later "discovered" then means someone else sold it
first.

### FleetTracker

`LoadGame` and `Loadout` identify the ship being flown. `Loadout` gives the full fit: engineering,
experimental effects, the grade's `Quality`, progress within it from 0 to 1, and the engineering's modified
statistics; the main and reserve tanks' capacities; the unladen mass; the cargo capacity; and the game's own
maximum jump range. It is kept whole as the ship's `LoadoutJson` so it can be handed to EDSY unchanged.
`Docked`, `Undocked`, `Location` and the jump events track where the player is, which is where the current
ship is.

The fleet and storage:

- `StoredShips` lists every owned ship with its location. `ShipyardSwap`, `ShipyardNew`, `ShipyardSell` and
  `SetUserShipName` keep the list current between those snapshots. `ShipyardTransfer` marks a ship in transit
  to the current system and station.
- `StoredModules` lists every stored module with its system, blueprint and grade, but not its experimental
  effect. The game writes it only when the storage screen is opened, so storage is kept current between
  snapshots by the events that move modules:
  - `ModuleStore` and `MassModuleStore` add what they store;
  - `ModuleBuy` adds the module the bought one displaced, and `ModuleBuyAndStore` a module bought straight
    into storage;
  - `ModuleRetrieve` removes what it fetches and adds what it swaps out;
  - `ModuleSellRemote` removes what it sells;
  - `FetchRemoteModule` marks a module in transit to the current system.
- A module that leaves a ship takes the engineering the ship's record of its slot carried, experimental effect
  included, which the storage snapshot alone cannot give. That effect is remembered: a later snapshot's module
  with the same item, blueprint and grade, at the same market or failing that anywhere, gets it back, so a
  stored module stays `ExperimentalKnown` once its effect has been seen. One stored before the journal history
  began, or on a ship the history never saw, stays unknown.

Engineering and bundles:

- The game writes a `Loadout` after every outfitting change but none after engineering, so `EngineerCraft` is
  applied to the module in the slot it names on the ship being flown: its blueprint, grade, experimental
  effect and modified statistics replace what the fit held. The ship's `LoadoutJson` is still the last
  `Loadout` as the game wrote it, so it lags that engineering until the next one.
- `ShipRedeemed` marks a ship redeemed from a bundle. The first `Loadout` after the redemption shows the
  bundle: a module listed without a `Value` is one the ship came with, flagged `IsPrebuilt`, and that fit is
  kept as the ship's `RedeemedModules`, so a page can say whether the ship is still as it came.
- From then on a slot stays prebuilt only while it holds that same item, still without a `Value`, and no
  `ModuleBuy`, `ModuleSell`, `ModuleStore` or `ModuleRetrieve` has touched it; a `ModuleSwap` moves the marks
  with the modules. A `Value` proves a bought module, but a missing one on a later `Loadout` proves nothing by
  itself, since the game also omits it for modules worth nothing, such as a surface scanner or the stock fuel
  tank. A redemption from before the journal history began is not seen, and the ship reads as bought.

### ActivityTracker

Keeps a feed of recent events as structured items: a kind, a subject and a short detail, with the
journal's localised names where it offers them. Arrivals, jump targets, docking, landings, discovery
scans, detailed body scans, surface signals, mapping, organic samples, exobiology and cartographics sales,
materials collected, engineering done, stored-module listings and ship swaps all appear. The feed holds
sixty items; older ones fall off.

### StatisticsTracker

`Statistics`, which the game writes each time the player loads in, is kept whole as sections of numeric
figures in the journal's order, so a page can show every lifetime figure without naming them in advance.
`Figure` looks one up by section and key, case-insensitively.

Income is a ledger of `IncomeEntry` records, one per payment, with a kind, the credits, the part that was a
bonus, and a detail. A payment of zero is not recorded, except a trade that broke even. The kinds by event:

- `RedeemVoucher` is bounties or combat bonds by its type, trading for a trade dividend, else other.
- `SellExplorationData` and `MultiSellExplorationData` are exploration, with the first-discovery bonus set
  apart.
- `SellOrganicData` is one exobiology entry per sample, named by species, with the first-logged bonus set
  apart.
- `MarketSell` is trading profit, the sale less the price paid, or mining and salvage when nothing was paid.
- `MissionCompleted`, `SearchAndRescue` and `PowerplaySalary` are what they say.
- `DatalinkVoucher`, `SellMicroResources` and `SellDrones` are other.

A second ledger of `SharedEntry` records holds only the income the game marks as shared with other players.
Each carries the payment, whether it was shared across a wing or a crew, and who was present. The rules are:

- A `Bounty` with a `SharedWithOthers` count is shared across the wing, as the voucher at the kill.
- Any `Bounty` or `FactionKillBond` earned in a crew is shared with the crew, since multicrew shares combat
  vouchers and nothing else.
- A `RedeemVoucher` of type `trade` is a trade dividend, the wing's cut of a wingmate's sale. It is redeemed
  later, so its partners are whoever is in the wing then, often nobody.
- A `MissionCompleted` whose `MissionAccepted` carried `Wing` is a wing mission.

Every `MissionAccepted` that carries `Wing` is also kept as a `WingMission`: its name, faction, reward,
when it was accepted and who was in the wing then. The journal does not say whether the player took it from
the board or a wingmate shared it, so both are listed. `MissionCompleted`, `MissionFailed` and
`MissionAbandoned` settle it. The `Missions` list the game writes on loading in settles the rest: a mission
under `Failed` has failed, one under neither `Active` nor `Complete` has lapsed, and one under `Complete` is
still awaiting its reward and stays active.

Income merely earned while in a wing is not shared. Shared bounties and bonds appear in this ledger as the
voucher and in the full ledger as its later redemption, so the two ledgers are not summed. The wing and crew
events are followed by `CompanyLog`:

- a wing opens at `WingJoin` or `WingAdd` and closes at `WingLeave`;
- a crew opens at `JoinACrew` or `CrewMemberJoins` and closes at `QuitACrew`, `EndCrewSession` or when the
  last member leaves;
- `LoadGame` and `Shutdown` close both, because a crash writes no leave event.

The snapshot carries every session with its partners, start and end, and the timestamp of the last event read,
which is what an open session is measured to.

The Powerplay standing comes from `Powerplay` at each load, `PowerplayRank` and `PowerplayMerits` keep it
current, and `PowerplayLeave` or `PowerplayDefect` clears it. `PowerplayMerits`, `PowerplayDeliver` and
`PowerplayCollect` are recorded as activity with their merits or units.

### UnlockTracker

| Event | Effect |
| --- | --- |
| `TechnologyBroker` | Records `BrokerType`, lower case, and every `ItemsUnlocked` name as a canonical symbol, as broker evidence. |
| `ModuleBuy`, `ModuleBuyAndStore` | Records `BuyItem` as a canonical symbol, as outfitting evidence: the game sells a module only once it is unlocked, fitted or straight into storage. |

`UnlocksSnapshot.Find` returns the first record naming an item that starts with one of a set of prefixes,
broker evidence first, and `HasItem` says whether there is one; that is how the application tells a bought
unlock from one still to buy. The game writes which items a purchase made available, never which unlock it
was, so an unlock whose item symbol is not known cannot be detected. The outfitting evidence matters because
the game keeps only recent journals: a purchase made before the oldest one is gone, but the modules bought
on its strength are not.

### CraftTracker

| Event | Effect |
| --- | --- |
| `EngineerCraft` | Records `Module` as a canonical symbol, `Slot`, `BlueprintName`, `Level`, `Quality` clamped to 0–1, and `ExperimentalEffect`, or `ApplyExperimentalEffect` when only that is written. |

`Quality` is the grade's progress after the roll: 0.5 after the first of two rolls at grade 2, 1 when the
grade is complete. `CraftsSnapshot.After` gives the rolls strictly after an instant, which is how the
application applies each roll to its plan once. The game writes no `Loadout` after engineering, so these
events are the only record of engineering as it happens; `FleetTracker` reads the same events to keep the
fitted module current.

### CarriedDataTracker

| Event | Effect |
| --- | --- |
| `Location`, `FSDJump`, `CarrierJump` | Remembers the current system for scans that omit it. |
| `Scan` | Adds the body to its system, with class, mass, terraform state and the `WasDiscovered`, `WasMapped` and `WasFootfalled` flags. A belt cluster, which has neither a star type nor a planet class, is skipped, and so is a scan from a nav beacon, `ScanType` `NavBeacon` or `NavBeaconDetail`: the game pays nothing for data it handed over. |
| `SAAScanComplete` | Marks the body mapped. |
| `SellExplorationData`, `MultiSellExplorationData` | Removes the systems named in `Systems` or in `Discovered`, so a partial sale keeps the rest, and records the sale in the ledger: the systems removed with their bodies, and the game's `BaseValue`, `Bonus` and `TotalEarnings` for the whole sale. |
| `ScanOrganic` with `ScanType` `Analyse` | Adds the sample once per species per body, with `WasLogged`: whether someone had logged that species there. |
| `SellOrganicData` | Records each species in `BioData` against the oldest unmatched sample of that species aboard, with its `Value` and `Bonus`, which is how a sale is credited to a system and a body; a species with no sample aboard is recorded with neither. Then clears every sample; Vista Genomics buys them all at once. |
| `Died` | Clears everything aboard; the game loses unsold data with the ship. The ledger stands. |

The game writes a `Scan` only for a body new to the player's data, so a body scanned on an earlier visit
is never counted twice. `Snapshot()` is what is aboard and `Sold()` the ledger. `CarriedData` knows nothing
about prices; the application values it, and shares a sale of several systems among them, since the game
reports one total for the whole sale.

## Backing the folder up

`JournalBackup.Copy` mirrors the journal folder, subfolders included, into a backup folder. A file is copied
when the destination lacks it, its size differs, or the source was written more than two seconds after the
copy; everything else is skipped, so a run after the first is quick. Files the game holds open are read with
sharing allowed, so the live journal and the status files copy while the game runs. Each copy keeps the
source's last-write time. When the source is shorter than the copy, the game has rewritten the file, and
the longer copy is renamed with a `.rewritten-<time>` suffix before the new content is copied, so the lines
the game discarded survive in the backup. Nothing in the destination is ever deleted. A file that cannot be copied is
reported in the result, not thrown; a destination that is the source or inside it is refused.

### Item symbols

Item symbols are read in canonical form. `Loadout` spells a module `Int_CargoRack_Size3_Class1`; the
storage events spell the same module `$int_cargorack_size3_class1_name;`. `JsonRead.Symbol` lowercases
the text and strips the `$` prefix and `_name;` suffix, so every tracker field holds
`int_cargorack_size3_class1` and a stored module matches a fit that asks for it.
