# Katey.Core

The domain. It holds the data catalogs and every rule that turns journal facts and plans into
materials, engineers, values and missing modules. It has no UI and no file IO beyond the JSON tables
embedded in the assembly, so everything in it is tested as plain objects.

## File map

| Area | Types | Purpose |
| --- | --- | --- |
| `Model.cs` | `Material`, `Blueprint`, `Experimental`, `ModuleType`, `EngineerProfile`, `Unlock`, `BrokerKind`, `MaterialUse`, `EngineerOffer`, the enums | The engineering records, and the technology broker unlocks with their material and commodity costs; `Unlock.Covers` says whether an item symbol is one the unlock makes available. `Blueprint.ClampGrade` picks the nearest grade a blueprint has. An engineer carries the position of their system. |
| `Catalog.cs` | `Catalog`, `CatalogException` | Loads the engineering tables, validates them, and answers lookups: blueprints and experimentals per module type, engineers per grade, materials by name or by journal symbol, the module types a blueprint applies to, the blueprint grades and experimentals that use a material, everything one engineer offers, and the unlocks of one module type or using one material. |
| `CatalogValidator.cs` | `CatalogValidator` | The rules the engineering tables must satisfy, one sentence per broken rule. |
| `Data/` | `MaterialRow`, `BlueprintRow`, `ExperimentalRow`, `ModuleTypeRow`, `EngineerRow`, the seven JSON files | The tables as the files spell them. |
| `ItemResolver.cs` | `ItemResolver` | Maps an EDSY or journal item symbol to a module type code. |
| `Search/TextIndex.cs` | `TextIndex<T>`, `SearchHit<T>` | A small name index: items under the text they are known by, queried word by word, best match first. |
| `Exploration/GalacticPosition.cs` | `GalacticPosition` | A point in the galaxy in light years from Sol, with distances. |
| `Planning/PlanEntry.cs` | `PlanEntry`, `PlanSource` | One module to engineer: blueprint, target grade, rolls per grade, experimental, enabled flag, and the instant after which the game's rolls count toward it. `IsSameWorkAs` tells two entries for one job apart from two jobs. |
| `Planning/PlanProgress.cs` | `PlanProgress`, `Craft`, `PlanAdvance` | Moves a plan on by the engineering rolls the game reported: rolls count down, and a finished entry leaves the plan. |
| `Planning/MaterialCalculator.cs` | `MaterialCalculator`, `MaterialTotal`, `MaterialLine` | Sums entries into totals and lays an inventory against them. |
| `Planning/EngineerCoverage.cs` | `EngineerCoverage`, `BlueprintCoverage`, `EntryService` | What a set of engineers unlocks, and who can serve one entry. |
| `Planning/ShoppingList.cs` | `ShoppingList` | Plain-text formatting of material lines, with a "Buy at a market" section for the commodities unlocks need. |
| `Planning/UnlockBudget.cs` | `UnlockBudget` | What a technology broker unlock costs against the inventory the plan leaves: the remaining counts, the lines, and whether it can be afforded. |
| `Trading/MaterialTrader.cs` | `MaterialTrader`, `TradePlan`, `MaterialTrade`, `TradeTarget`, `TradeShortfall`, `TradeRate`, `MaterialReserve`, `MaterialCaps` | The material traders' exchange rates, the storage caps, the reserves a player keeps, and the planner that says what to hand over for what. |
| `Edsy/EdsyImport.cs` | `EdsyImport`, `ParsedLoadout`, `ImportedModule`, `LoadoutModule`, `EdsyParseResult` | Parses an EDSY journal Loadout export, jump profile included when the export carries the masses. |
| `Exploration/StarClasses.cs` | `StarClasses`, `StarKind` | Scoopable, neutron, white dwarf, black hole, or other, from a star class string. |
| `Exploration/BodyValue.cs` | `BodyValue` | The cartographics payout estimate. |
| `Exploration/SamplingTime.cs` | `SamplingTime` | How long a landing takes by the species to sample, what it pays a minute, the best landing among them, and the odds of one worth the stop. |
| `Exploration/SurfaceGravity.cs` | `SurfaceGravity`, `GravityBand` | Light, high or extreme surface gravity, for a landing warning. |
| `Exploration/ExobiologyPredictor.cs` | `ExobiologyPredictor`, `BodyConditions`, `GenusPrediction`, `SpeciesPrediction`, `GenusLikelihood` | Which species a genus signal is likely to be, what it pays, and which genera have ever been found under a body's conditions. |
| `Fleet/ModuleCatalog.cs` | `ModuleCatalog`, `ModuleInfo` | Every purchasable module by item symbol, for naming, and the modules of one module type. `NameOf` falls back to `ModuleSymbolNames` for a symbol the table lacks. Rows whose symbol contains `_missing_` are coriolis's placeholders for unrecognised items; `ModuleInfo.IsPlaceholder` marks them and `OfType` leaves them out. `IsCosmetic` names the paint jobs, decals, nameplates, bobbleheads, ship kits, colour packs, voice packs and cockpit entries the journal lists among a ship's modules. `IsFixture` names the cargo hatch and the planetary approach suite, which every ship carries. |
| `Fleet/FitMatcher.cs` | `FitMatcher`, `Fitting`, `FitModule`, `Engineering`, `OwnedModule`, `SharingPolicy`, `MatchReport`, `FitReport`, `FitLine`, `MatchQuality` | Saved fits against owned modules, and the best owned module for one requirement. |
| `Fleet/FitComparer.cs` | `FitComparer`, `FitComparison`, `FitDifference`, `DifferenceKind` | One fit against one ship's actual modules, slot by slot. |
| `Fleet/ModuleSymbolNames.cs` | `ModuleSymbolNames` | Names for symbols the module table lacks: hull armour by grade, and compound symbols such as `int_largecargorack_size6_class1` read as words over a lexicon built from the table's own names. `Canonical` reduces every spelling of an item to one symbol: lower case, no `_free` or `_default` suffix, and every ship-specific cargo hatch as `modularcargobaydoor`. |
| `Fleet/ShipNames.cs` | `ShipNames` | Display names for hull symbols such as `anacondamkii`. |
| `Fleet/SlotNames.cs` | `SlotNames`, `SlotKind`, `SlotPlace` | Readable slot names such as "Large Hardpoint 3", and the kind and size a slot name denotes. |
| `Edsy/EdsyExport.cs` | `EdsyExport` | Writes a build as a journal `Loadout`, the form EDSY imports. |
| `Exploration/FuelModel.cs` | `FrameShiftDrives`, `JumpObservation`, `JumpFuelModel`, `FuelLeg`, `FuelStop`, `FuelProjection`, `FuelPlanner` | Drive facts, the fuel model, its fit to observed jumps, and the projection along a route. |
| `Routing/JumpProfile.cs` | `JumpProfile` | A ship's jump numbers, and its full-tank range by the game's equation. |
| `Routing/PlotRequest.cs` | `PlotRequest`, `PlotAlgorithm` | One ask of a galaxy plotter, and its form fields in Spansh's names. |
| `Routing/PlottedRoute.cs` | `PlottedRoute`, `PlottedStop` | A plotted route, and where a system is on it. |
| `Routing/SpanshJson.cs` | `SpanshJson`, `PlotJob`, `PlotJobStatus` | Reads Spansh's replies: queued, done with a route, or failed. No IO. |
| `Routing/SpanshBodiesJson.cs` | `SpanshBodiesJson` | Reads the bodies out of Spansh's dump of a system. No IO. |
| `Exploration/CataloguedBody.cs` | `CataloguedBody`, `CatalogueNames` | A body as a catalogue outside the game describes it, and the catalogues' names turned into the journal's spellings. |

## How materials are counted

A `PlanEntry` is one of two things. A module entry names a blueprint, a target grade, rolls per grade and
an experimental, and costs what those cost. An unlock entry names a technology broker unlock through
`PlanEntry.Unlock`, created by `PlanEntry.ForUnlock`, and costs the unlock's fixed materials; its
blueprint is empty, its grade 0 and its rolls empty, and `IsUnlock` tells the two apart. Both kinds sum
into the same totals. An unlock's commodities are not materials: `MaterialCalculator.Commodities` sums
them separately, since they are bought at a market rather than collected.

`PlanProgress.Apply` moves a plan on by the rolls the game reported, each a `Craft` in the catalog's terms:
module type, blueprint, grade, the grade's progress after the roll and the experimental effect then on the
module. A roll finds the first module entry for its blueprint and module type that it is later than, by the
entry's `CraftsAfter`; an entry whose module type is unknown takes any module of its blueprint. The roll then:

1. zeroes the rolls still planned at every grade below the one rolled, since the module is past them;
2. takes one roll off the grade rolled, or all of them when the grade's progress reached 1;
3. drops the entry's experimental effect once the module carries it, so its materials stop counting;
4. moves the entry's `CraftsAfter` to its time, so it is never applied twice.

The entry is done, and leaves the plan, when every planned grade is rolled out, the module stands at the
target grade or above, and no effect is still wanted; an experimental-only entry is done when the effect is.
Unlock entries are never touched, nor is an experimental-only entry with no effect chosen.
`PlanProgress.Floor` is the earliest `CraftsAfter` among module entries, so a caller can skip the rolls before
it. The rule cannot tell which ship was rolled: a roll on any module of the blueprint and type advances the
entry.

`UnlockBudget` answers whether an unlock can be afforded out of what the plan leaves: `Remaining` takes
the inventory and subtracts every enabled entry's materials, floor zero, and `CanAfford` checks one
unlock's cost against that. Planning one unlock can therefore put another out of reach, and dropping it
brings the other back.

A plan entry names a blueprint and a target grade. Its materials are the sum, over every grade the blueprint
has from 1 up to the target, of that grade's materials times the entry's roll count for that grade, plus the
experimental effect's materials once. Roll counts default to the grade's number, one at grade 1 up to five
at grade 5, which is about what it takes to bring a grade to its full effect before moving up. They can be
any non-negative whole number. A target grade of 0 means experimental only: the module already has the
blueprint, no grade is rolled, and the entry costs the effect's materials alone. Anyone who can apply the
effect counts as able to serve such an entry. A disabled entry contributes nothing. Totals are sorted by
inventory, then family, then grade, then name.

`MaterialCalculator.WithInventory` lays a symbol-keyed inventory against the totals. The inventory dictionary
must compare keys case-insensitively, because the journal spells symbols in lower case and the tables in
mixed case. A line's `Missing` is needed minus have, never negative; with no inventory it equals the need.

## Trading at the material traders

`TradeRate.Between` gives the game's exchange rate between two materials of the same inventory, as a whole
batch: give so many, get so many. Within one family, going up costs 6 per grade and going down returns 3
per grade, both compounding. Crossing to another family costs a flat factor of 6 on top of that. Raw,
manufactured and encoded never trade for one another, so the rate between them is `null`. The rates come
from the Elite Wiki's description of the traders, recorded in [`NOTICE.md`](../../NOTICE.md).
`MaterialCaps.For` gives the storage cap by grade: 300, 250, 200, 150 and 100.

`MaterialTrader.Plan` takes the plan's needs by material name, the inventory by journal symbol and the
player's `MaterialReserve` records, and returns a `TradePlan`. Each material has a floor: the larger of
what the plan needs and the reserve, no more than the cap. The targets are the materials held below their
floor, worked highest grade first. Only what is held above a floor is ever handed over, and a material
marked never-trade is not spent at all.

For each target the planner scores a trade from every spare material in the same inventory:

- The base score is worth lost. A unit is worth three of the grade below, so trading down a family loses
  nothing, trading up loses half per grade, and crossing families loses five sixths.
- The score shrinks the fuller the source is above its floor, by up to half at the cap, because a full
  stack can take no more and anything found later is lost.
- A source that cannot cover the whole shortfall on its own is surcharged one fifth, for the extra trade
  it leaves the player to make.
- Ties go to the trade that hands back the least beyond what is wanted, then to the one that hands over
  the fewest units.

The lowest score wins. When it leaves a remainder the planner goes again for the rest. When nothing can be
traded it records a `TradeShortfall` with the reason:

- the only spare materials are never-trade ones;
- nothing is held above its floor;
- nothing else in the inventory is held;
- every whole batch would overfill the target's stack.

Batches are whole and never overfill a stack, so a trade down a grade can hand back a few more than wanted;
each `MaterialTrade` reports that surplus.

## Who can do the work

An unlock entry has no engineer: a technology broker sells it, so `EngineerCoverage.ServiceFor` reports
nobody for it and the plan raises no engineer warning.

An engineer appears against an entry when the tables list them for that module type, blueprint and grade.
Experimental effects follow the game's rule: any engineer who offers any grade of any blueprint for a module
type can apply any of that type's experimental effects. `EngineerCoverage.For` applies the same two rules to
every blueprint for a selection of engineers. `EngineerCoverage.Reach` gives the highest grade a selection
reaches on one blueprint for one module type, and `StartingGrade` is that grade, or the blueprint's top grade
when nobody selected offers it. Four more narrow the catalog's lists to what the selection can roll:

- `Blueprints`, the blueprints of a type;
- `Grades`, the grades of a blueprint up to the reach;
- `ModuleTypes`, the types a blueprint can be rolled on;
- `OffersAny`, whether the selection can apply a type's experimental effects at all.

## What the EDSY import reads

`EdsyImport.Parse` accepts an EDSY "Export to journal Loadout" in any of the shapes EDSY and the game journal
produce: an array of `{ header, data }` objects, one such object, or a bare Loadout. Every module with an item
goes into `AllModules`, for fits. Only modules with an `Engineering` block go into `Modules`, for the plan.
There `BlueprintName` must be a blueprint key and `Level` must be 1–5, or the module is skipped with a
warning; an `ExperimentalEffect` the tables do not know is dropped with a warning while the module is still
imported. The module type comes from `ItemResolver`; an item no pattern matches is imported with its materials
but with no module type, so no engineers can be listed for it.

When the export carries `UnladenMass` and `FuelCapacity`, which EDSY writes as the game does, `Profile` is the
loadout's `JumpProfile`, built from those and each module's `Engineering.Modifiers`; without them it is
`null`. `EdsyImport.ProfileOf` reads that profile alone from an export text, without a catalog, and answers
`null` for anything that is not a Loadout with the masses and a drive.

## Exploration

`StarClasses.Classify` reads the class strings the journal and `NavRoute.json` use:

- O, B, A, F, G, K and M are scoopable, and so is a giant of one of those classes, which the game spells with
  a suffix such as `M_RedGiant` or `A_BlueWhiteSuperGiant`;
- N is a neutron star;
- any class starting with D is a white dwarf;
- H and `SupermassiveBlackHole` are black holes;
- everything else is other.

`Describe` and `Brief` put a class in a reader's words, "Class M red giant star" and "M red giant". `Spectral`
gives a scoopable star's letter, which is how a giant takes its class's colour.

`SurfaceGravity.Band` reads a surface gravity in g: from 1.0 g a landing is high, from 1.5 g extreme, where a
heavy ship may not arrest its descent. The thresholds are the two constants on the class.

`BodyValue` is the community's reverse-engineered cartographics formula: a base value per body type, raised
for terraformable bodies, scaled by mass to the power 0.2, multiplied for mapping and again for first
discovery, never below 500 credits. It is an estimate; the game's figure can differ by a few percent.

`ExobiologyPredictor.Predict(genus, body)` scores every species variant of the genus by the geometric mean
of how often it has been found under the body's atmosphere, body type, volcanism, gravity, temperature and
pressure, weighted by how often the variant has been seen at all. Scores are normalised within the genus,
candidates under one percent are dropped, and variants are folded into species. A category the body has
that the variant has never been found under scores low rather than zero, so sparse data does not rule a
species out. The journal and Bioforge spell atmospheres, body types and volcanism differently;
`Canonical` reduces both to one key. Rewards are Vista Genomics' base payouts without the first-discovery
bonus.

`CanLive(genus, body)` asks a stricter question with no smoothing: whether some species of the genus has
been recorded with the body's atmosphere, body type and volcanism and within its gravity, temperature
and pressure ranges. `PossibleGenera(body)` lists the genera that pass. A body with signals but no genus
list yet is bacteria-only when bacteria is the only genus that passes; one that another genus could live
on stays undecided until it is mapped.

`LikelyGenera(body)` weighs the genera that pass against each other. A genus's weight is the sum over its
variants of how often each has been seen times its fit to the body's conditions, the same geometric mean
`Predict` uses, normalised across the genera so the shares sum to one. A body's expected payout per
signal before mapping is each genus's expected reward times its share, which is what weights bacteria,
found almost everywhere, above a genus seen a handful of times under such conditions.

## Fits

A `Fitting` is a hull and the modules a saved EDSY export fills it with. An `OwnedModule` is one module
the player has, fitted to a ship or in station storage, with its engineering as far as the journal reports
it. `FitMatcher.Match` works through the fits in order; within one fit every module is used once, and the
sharing policy decides whether a module one fit used is still available to a later one:

| Policy | Rule |
| --- | --- |
| `Pooled` | Modules move between ships as needed. Every fit may use every owned module. |
| `SameHull` | Modules stay with a hull type. Fits for the same hull reuse a module; fits for another hull cannot. |
| `None` | Every fit needs its own copy. |

Items are compared through `ModuleSymbolNames.Canonical`, so a module that came free with a bundled ship,
which the game writes with a `_free` suffix, or a ship-specific cargo hatch such as `modularcargobaydoorfdl`,
is the item the fit names. A stock requirement accepts any module of the item. An engineered requirement needs
the same blueprint at the required grade or higher, and the same experimental effect when one is required.
`Engineering.Quality` is the game's progress within the grade. A module at the required grade with that below
1 is partial, since the grade is not rolled to its full effect; `FitComparer` reports that case as
`UnfinishedGrade`. A module engineered before the game kept the figure, which it reports as 0, counts as
complete. Station storage records blueprint and grade but not the effect, so such a match is reported as
`ExperimentalUnknown`.

Among modules that match equally well, the order is:

1. one already fitted to a ship of the fit's hull, then one in storage, then one fitted to a ship of another
   hull, which would have to give it up. So a fit is met by its own ship's modules before it borrows, whatever
   order the fits or the modules come in;
2. within a place, the nearer module when the caller says how far each is, with the player's own system
   nearest and an unknown place last;
3. the least wasteful module, a stock one for a stock need, so engineered modules are kept for the fits that
   need them.

A module flagged `IsPrebuilt` cannot leave its ship, so it is offered only to fits of its own hull, and never
to `Find`, which has no hull in play. Fits for hulls the player does not own are reported as such with nothing
assigned. The report ends with every owned module no fit claimed. `FitMatcher.Find` answers the same question
for one requirement against any set of owned modules, with no fit or policy in play.

`FitComparer.Compare` lays a fit against the modules actually fitted to one ship. A required item is looked
for in the slot the fit names first, then in any other slot of the same kind, nearest in size first, so a
module the ship carries in a different optional bay or hardpoint still counts. `SlotNames.Parse` reads the
kind and size from a slot name: core, hardpoint, utility, optional, military or other. Each fitted module
satisfies at most one requirement. A stock requirement is satisfied by any module of the item. An engineered
requirement is satisfied by the same item with the same blueprint at the required grade or higher and, when
the fit names an experimental effect, the same effect. Every other case is a `FitDifference` with a
`DifferenceKind`:

- the slot is empty, or its module is spoken for;
- it holds a different item;
- it is not engineered, has a different blueprint, or a lower grade;
- it has a different or missing experimental, or one the journal does not know.

A difference knows whether closing it means buying, engineering, or only applying the experimental, and
whether the module it judged sits in another slot.

## Fuel

`FrameShiftDrives` answers for a drive by its item symbol from `drives.json`: the exponent and the multiplier
of the fuel equation, the most fuel one jump may burn, and the optimal mass of the unmodified drive. It also
gives the neutron multiplier, 6 for the size 8 Mk II and 4 for every other. The table is the one Spansh's own
plotter works from, so a plan asked for through Katey matches one asked for on the site. The class also holds
the range a Guardian booster adds by size and the range multipliers of a neutron star and a white dwarf.

`JumpFuelModel` is `fuel = k × effective^power`, capped at the drive's maximum. The effective distance is the
jump divided by the departure star's boost, then scaled by the drive's own range over its range with the
Guardian booster. The booster thus stretches every jump by the same factor; the game charges for it that way,
not as a flat slice off the distance. The scale `k` comes one of two ways. `JumpProfile.FuelModel` computes it
as the game does, from the loadout's masses and the drive's optimal mass: the multiplier times the ratio of
the mass aboard to the optimal mass, raised to the power. `JumpFuelModel.Calibrate` fits it instead to the
jumps the ship has actually made, for a loadout the journal has not given the figures for. The fit is the
median of the per-jump fits, settled over a few rounds since the booster's share depends on the range, and it
folds the ship's mass and the drive's engineering into one number without knowing either.

`FuelPlanner.Project` runs the model along the legs ahead from the tank reading, refilling to capacity at
every scoopable stop. It asks for the model before each jump with the fuel then aboard, so a model built
on the mass, as `JumpProfile.FuelModel` is, follows the ship as it lightens; a fixed model is the same
for every jump. It reports fuel on arrival at each stop and what the jump into it costs, how many
jumps the tank covers with no scoop, the last scoopable stop reachable on that tank, and whether the route
runs dry even when scooping wherever it can.

## Routing

`JumpProfile` is what a route plotter needs to know about a ship: the drive's exponent and multiplier, its
optimal mass and most fuel per jump with engineering applied, the base mass, the main and reserve tanks, a
Guardian booster's range and the neutron multiplier. `Range` gives the full-tank jump range with a given
cargo. `MaxRange` gives the range with one jump's fuel aboard, which is the figure the game calls the maximum
jump range. `FuelModel` gives the fuel equation at a given load, so the burn is known before a jump is made.

`Build` makes one from a loadout's figures, the same for the game's `Loadout` event and EDSY's export of one.
The first module that is a frame shift drive sets the drive figures, with `MaxFuelPerJump` and
`FSDOptimalMass` from its modifiers replacing the stock ones, and a Guardian booster anywhere adds its range.
When the loadout carries the game's own `MaxJumpRange`, the fuel cap is solved from it so that `MaxRange` with
no cargo equals the game's figure; the game's number is the authority over the table. A saved `Fitting` keeps
the export text it was pasted from as `LoadoutJson`, so the same text goes back to EDSY unchanged and
`EdsyImport.ProfileOf` reads its profile. That is how a route can be plotted for a fit as well as for a ship.

`PlotRequest` is one ask of Spansh's galaxy plotter: the two ends, the profile, the cargo and the options.
`FormFields` writes it in the field names the service takes. `SpanshJson.Read` turns a reply into a
`PlotJob`: queued with a job id, done with a `PlottedRoute`, or failed with the reason. A `PlottedRoute`
holds every stop with its position, jump distance, distance left, fuel on arrival and whether it is a
neutron star, scoopable or a refuel stop. `IndexOf` finds a system on it by id or by name. Nothing here
touches the network; the client lives in the App layer.

## The data tables

Nine JSON files in `Data/`, embedded in the assembly. Each is an object keyed by the identifier the other
tables use to refer to it, and only fields the app uses are kept.

| File | Keyed by | Record |
| --- | --- | --- |
| `materials.json` | display name, as blueprints spell it | `symbol`, `name`, `category` (raw, manufactured, encoded), `grade` 1–5, `line` (in-game family) |
| `blueprints.json` | journal name such as `FSD_LongRange` | `name`, `grades`: grade key → material name → count, only the grades that exist |
| `experimentals.json` | journal name such as `special_fsd_heavy` | `name`, `components`: material name → count (empty for the three Legacy effects) |
| `moduleTypes.json` | short code such as `fsd` | `name`, `group` (core, optional, hardpoint, utility), `itemPatterns`, `blueprints`: blueprint key → grade key → engineer names, `experimentals` |
| `engineers.json` | engineer name | `name`, `system`, `base`, `region` (bubble, colonia), `x`, `y`, `z` (the system's galactic coordinates, light years) |
| `modules.json` | item symbol, lower case | `symbol`, `name` with size and rating, `group` (module type code), `class`, `rating`, `mount` |
| `unlocks.json` | stable key such as `guardian_fsd_booster` | `name`, `broker` (guardian, human), `category` (modules, weapons, modifiedWeapons, fighters, preEngineered), `moduleType` (code, or null for a fighter), `items` (lower-case item symbol prefixes the purchase makes available, for the journal's `TechnologyBroker` event; empty when unknown), `materials`: material name → count, `commodities`: commodity name → count |
| `drives.json` | item symbol, lower case | `symbol`, `size`, `rating`, `optimalMass`, `maxFuelPerJump`, `fuelMultiplier`, `fuelPower`, `superchargeMultiplier`: every frame shift drive, standard, SCO and the size 8 Mk II |
| `exobiology.json` | species variant symbol | `name`, `species`, `speciesName`, `genus`, `reward`, `count`, histograms `atmospheres`, `bodies`, `volcanism` (category → finds) and `gravity`, `temperature`, `pressure` (bins of min, max, finds) |

`itemPatterns` are regular-expression sources matched case-insensitively against an item symbol. When
several module types match one item, the longest matching source wins, so `^int_shieldgenerator_.*_fast$`
(bi-weave) beats `^int_shieldgenerator_` (standard).

### Provenance

- **coriolis-data** ([github.com/EDCD/coriolis-data](https://github.com/EDCD/coriolis-data)): blueprints
  and their per-grade materials, experimental effects and their materials, which engineers offer each grade
  of each blueprint on each module type, and the module catalog with its display names and item symbols.
  Its `modules/standard/frame_shift_drive.json` supplies `drives.json`: optimal mass, fuel cap, fuel
  multiplier and exponent for every drive, SCO and Mk II drives included. Where it lists a symbol twice with
  different figures, the size 5A SCO drive, the first row is kept, which agrees with the other SCO drives.
  The Mk II's six-times neutron boost is the figure Spansh's own plotter page carries for it.
- **EDCD FDevIDs** ([github.com/EDCD/FDevIDs](https://github.com/EDCD/FDevIDs)), `material.csv`: the
  material catalog, with Inara supplying the grades of the caustic and Titan Thargoid materials that
  snapshot lacks. Its `outfitting.csv` supplies the module rows coriolis-data lacks: the MkII vessel
  hangars, the large and MkII large planetary vehicle hangars, the MkII passenger cabins, the size 6
  MkII cargo rack, the size 8 frame shift drives and the three xeno scanners. The rest of that list is
  hull armour, which `ModuleSymbolNames.ArmourName` already names, the discovery scanners the game
  retired in 2018, and a sizeless hatch breaker stub whose sized rows the table has.
- **Canonn Bioforge** ([bioforge.canonn.tech](https://bioforge.canonn.tech/)), through the
  [Mirooz/EliteDangerousWarboard](https://github.com/Mirooz/EliteDangerousWarboard) mirror: the per-variant
  statistics behind `exobiology.json`.
- **EDEngineer** ([github.com/msarilar/EDEngineer](https://github.com/msarilar/EDEngineer), code MIT),
  `blueprints.json` and `entryData.json`: the technology broker unlocks and what each costs, which tell a
  material from a commodity. The module type each unlock belongs to, its category, and the item symbols
  the purchase makes available are hand-authored.
- **Hand-authored**: each module type's group, and every engineer's system, base, region and system
  coordinates. The coordinates are the game's, on its 1/32 light year grid. The application prefers the
  coordinates the player's own journal recorded for a system once the player has been there, so a wrong
  table value corrects itself on arrival.

The conversion from upstream applies these rules:

- coriolis's `MC_Overcharged` entry is merged into `Weapon_Overcharged`. Both carry the journal name
  `Weapon_Overcharged` and identical materials at every grade.
- coriolis's `mr` (missile rack) is split into `mr` (seeker, `hpt_basicmissilerack`) and `dmr` (dumbfire,
  `hpt_dumbfiremissilerack`), so every module type has one experimental list. `amr` (advanced missile rack)
  keeps the dumbfire list.
- All 87 coriolis module types are kept, 88 after the split, plus five that coriolis-data's module files
  define but its engineering table lacks (multi-limpet, recon and research limpet controllers, supercruise
  assist, mining volley repeater), for 93. The 48 the game does not let you engineer have empty
  `blueprints` and `experimentals`; the picker lists only types with a blueprint, and the import uses the
  full set so it can name a module that cannot be engineered. Two codes (`mh`, `mm`) are coriolis's
  placeholders for unrecognised items and are kept as such. The code `gpl` has no name in coriolis; it is
  read as the Guardian power distributor, the one Guardian core module the table would otherwise lack.
- Two upstream typos are corrected: the material `Adaptive Encyptors Capture` is `Adaptive Encryptors
  Capture`, and the engineer `Felicty Farseer` is `Felicity Farseer`. A duplicate Etienne Dorn in life
  support grade 5 is removed.
- `modules.json` takes each coriolis module's `name` (or `ukName`, or its group's name) with its class,
  rating and mount appended, and skips coriolis's pre-engineered variants, which reuse the standard
  symbols. A row taken from FDevIDs is named the same way, "MkII Vessel Hangar 5D", under the module type
  code its symbol pattern resolves to; the `_free` rows are not copied, since `NameOf` reads a free
  module as its bought twin.
- `exobiology.json` takes every species file in the mirror's `species` folder, fifteen genera from Aleoida to
  Tussock, and keeps, per variant, the reward, the find count and six of Bioforge's histograms; the
  surface-material, distance, primary-star and system-body histograms are dropped. Species and genus
  symbols are derived from the variant symbol, so `$Codex_Ent_Bacterial_01_O_Name;` belongs to species
  `$Codex_Ent_Bacterial_01_Name;` and genus `$Codex_Ent_Bacterial_Genus_Name;`, which is how the journal
  names them.
- `unlocks.json` takes EDEngineer's `Guardian` and `Human` blueprint rows. Its "Blueprint Segment"
  materials are the catalog's "Blueprint Fragment" ones; an ingredient EDEngineer's entry data calls a
  commodity goes under `commodities`; the three fighters and the corrosion-resistant cargo rack take
  shorter names. A pre-engineered unlock whose journal symbol is not known has an empty `items` list, so
  the journal cannot report it bought; thirteen rows are like that.
- Dropped fields: blueprint `features`, `uuid`, `id`, `modulename`; special `id`, `uuid`, `description`;
  module `modifications`; material `elementSymbol`.

### Validation

`Catalog.FromRows` runs `CatalogValidator` and throws `CatalogException` listing every problem. The rules:

- every grade is 1–5;
- every category, group and region is one of its allowed values;
- every material a blueprint or experimental names exists;
- every blueprint and experimental a module type names exists;
- every grade a module type lists exists on that blueprint;
- every engineer named in a grade list exists and offers at least one blueprint;
- every `itemPattern` compiles and is unique;
- material symbols are unique.

`ModuleCatalog` and `ExobiologyPredictor` throw `CatalogException` when their tables are missing or malformed.
The `CatalogTests`, `FleetTests` and `ExplorationTests` suites load all three, so a data typo fails the test
run.

### Refreshing the data

Edit the JSON tables by hand following the convention and rules above, then run `dotnet test Katey.slnx`.
