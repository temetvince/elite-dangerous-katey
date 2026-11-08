# Katey manual

Katey is a desktop companion for Elite Dangerous. It reads the journal files the game writes on your own
machine and keeps up with you as you play: it plans engineering against the materials you actually hold,
checks your saved ship builds against the ships and modules you own, follows your route and the system you
are in, and reads your history as a ledger. Nothing leaves your machine unless you turn the Spansh route
plotter on, and then only when you press Plot.

## Contents

- [Quick start](#quick-start)
- [The window](#the-window): the [top bar](#the-top-bar), the [bottom bar](#the-bottom-bar),
  [search](#search), [the plan drawer](#the-plan-drawer) and [the live rail](#the-live-rail)
- [The pages](#the-pages): [Home](#home), [Engineers](#engineers), [an engineer](#an-engineer),
  [Modules](#modules), [a module type](#a-module-type), [a blueprint](#a-blueprint),
  [Materials](#materials), [a material](#a-material), [Unlocks](#unlocks), [Fleet](#fleet), [a ship](#a-ship), [a fit](#a-fit),
  [System](#system), [Route](#route), [Stats](#stats) and [Settings](#settings)
- [Working with EDSY](#working-with-edsy)
- [Prebuilt ships](#prebuilt-ships)
- [What the journal cannot tell Katey](#what-the-journal-cannot-tell-katey)
- [Your data](#your-data)

## Quick start

1. **Start Katey.** The release is a single executable; nothing needs installing. Only one Katey runs at a
   time: starting a second shows a notice and leaves the first alone, since two would overwrite each other's
   saved data. On Windows the game's
   journal folder is found by itself. If the Settings page says no folder was found, point it at the folder
   that holds files named `Journal.*.log`, usually under `Saved Games\Frontier Developments\Elite Dangerous`.
2. **Let it catch up.** The first run replays your whole journal history, oldest file first, and shows its
   progress along the top of the window and in the rail: how many files are read, with a bar that fills file by
   file. Nothing live appears until it finishes, so you never see a figure from partway through. A multi-year
   history takes a few seconds.
3. **Tick your engineers.** Open **Engineers** in the top bar and tick the engineers you have unlocked and
   intend to visit. Turn on **Show only items from selected engineers** at the top of that page and every
   list and picker in Katey narrows to what those engineers can roll, with each grade picker starting at the
   grade they reach.
4. **Plan some engineering.** Open **Modules**, pick a module type, choose a grade and an experimental on
   the blueprint you want, and press **Add**. Or open the **Plan** drawer from the bottom bar and paste an
   EDSY build into its import box; every engineered module in the build becomes an entry.
5. **Read the plan.** The drawer lists every entry with who can roll it, every material the plan needs
   against what you hold, and, at the bottom, the trades to make at the material traders to cover whatever
   you are short of. **Copy** puts the shopping list on the clipboard.
6. **Fly.** The live rail on the right follows the game: your position, the route, the fuel, the bodies
   worth scanning in this system, with the ones that carry a material you are short of tagged. **Pop out** moves it
   to a window of its own that stays over the game.
7. **Check a build against your fleet.** Open **Fleet**, paste an EDSY build into the saved fits box, and
   open the fit. It is compared slot by slot with the ship that matches it best and with everything you own,
   and one button plans the engineering gap.

Everything else in this manual says what each part of the window does and how to get the most out of it.

## The window

Katey is one window with a page in the middle, a rail along the right edge, and two bars. It draws its own
title bar in the app's colours and carries Katey's face as its icon. The pages are never narrower than 960
pixels, so the window's minimum width is that plus the rail while the rail is docked; pop the rail out and the
window fits a screen 1080 pixels wide. It opens centred on the primary screen, whichever screen it was last
on.

### The top bar

The top bar is the window's title bar: drag it to move the window, double-click it to maximise, and use the
three buttons at its right end to minimise, maximise and close. From left to right it holds:

- **KATEY**, the wordmark, which opens the [start page](#home).
- **Back** and **forward**, which work as in a browser and return you to where you had scrolled.
- The page links: **Engineers**, **Modules**, **Materials**, **Unlocks**, **Fleet**, **Stats**, **Route**
  and **System**. The one whose section you are in is lit, so an engineer's page lights Engineers and a
  blueprint lights Modules.

### The bottom bar

- **Settings** on the left opens the [Settings](#settings) page. Press it again while settings is up and
  you go back to the page you were on.
- The **search box** across the middle; see [Search](#search).
- **Plan** on the right opens and closes the [plan drawer](#the-plan-drawer). The number on it is how many
  entries the plan holds.

### Search

Type in the search box, or press **Ctrl K** to jump to it, to find an engineer, a module type, a blueprint,
a material, a system you have visited, one of your ships or a saved fit. The results rise above the box as
you type. **Enter** opens the first result, the arrow keys move through them, and **Escape** clears the
box. Every result opens its own page.

### Pages, links and remembered choices

Every page describes one thing or lists things of one kind. Every mention of something else on a page is a
link that opens its page: a system name opens the system, an engineer's name opens the engineer, a material
chip opens the material. The small labels above a page's title are links back up to the index it belongs
to.

Three mechanisms behave the same on every page, and each remembers its state between runs:

- **Sorting.** Click a column heading to sort the table by it; click it again to flip the direction.
- **Sections.** A long page shows one section at a time, chosen by the tabs under its title.
- **Groups.** A page that rolls things up by group opens and closes each group on a click, with
  **Expand all** and **Collapse all** beside them.

Filters, switches and the open state of every fold are remembered too, so a page comes back as you left it.

### The plan drawer

The plan is the list of engineering you mean to do. The drawer opens over any page from the **Plan** button
and holds:

- **The entries.** Each has:
  - a switch to leave it out of the totals without deleting it;
  - a grade picker and, for a type that takes experimental effects, an experimental picker and the
    **experimental only** switch; a type with none shows neither;
  - the rolls expected per grade;
  - the engineers who can roll it, with the ones you ticked marked. An entry nobody you ticked can roll
    carries a warning.

  The **experimental only** switch turns an entry into one that costs only the experimental's materials, for a
  module that already has its blueprint. A technology broker unlock you ticked on the [Unlocks](#unlocks) page
  is an entry too, with its fixed materials and the commodities it also needs from a market.

  The plan follows what you do at the engineer:

  - each roll the game reports on a module of an entry's blueprint and type takes one roll off that grade, or
    clears it when the grade completes;
  - the experimental effect comes off the entry as soon as it is on the module, whatever grade that is at;
  - once the module stands at the target grade with nothing left wanted, the entry leaves the plan.

  Only rolls made after the entry was added count, so engineering done earlier never removes an entry.
- **The totals.** Every material the plan needs, with how many you hold and how many you are short, grouped
  by raw, manufactured and encoded. **Copy** puts the list on the clipboard as plain text.
- **The trades.** What to hand over at the material traders to cover everything you are short of, trade by
  trade, grouped by trader. See [Materials](#materials) for how trades are chosen.
- **The EDSY import.** Paste a build exported from EDSY, press **Read**, untick any module you do not want,
  and press **Add**. See [Working with EDSY](#working-with-edsy).

**Clear** empties the plan after a second press, so one slip cannot lose it.

### The live rail

The rail follows the game along the right edge of the window. It shows:

- **Where you are** and what the ship is doing: docked, in supercruise, landed, and so on.
- **The route**, as the stop you are at and the next few, with the rest summarised, the next scoopable
  star and any engineer stop called out, the distance travelled against the whole and as a percentage,
  the tank bar as the [route page](#route) draws it, and the fuel picture: how many jumps you can make
  before you must scoop, and which star to scoop by. When a Spansh plot is held, the next waypoint shows
  with a **Copy** button for the galaxy map.
- **This system**: the scan progress and payout estimates, with every scanned body that carries surface
  materials or biological signals. The heading and the ring turn green once every body is scanned.
  - Before a body is mapped, its life is one line: how many genera could live there and what they are likely
    worth, with and without the first-logged bonus. Once mapped, each genus shows with its likeliest species.
  - Gravity shows as a tag from 1.0 g, red from 1.5 g.
  - A switch orders the bodies **nearest first**, by distance from arrival, or **biology first**: every body
    with life at the top, the most valuable first, then by signal count, with a body you have landed on at the
    bottom. The value is the scanner's genera once mapped, or the life the conditions allow before that.
  - A second switch hides bodies whose life is bacteria alone: by the scanner's genera once mapped, or before
    that a single signal at least four in five likely to be bacteria. A body carrying a material your plan is
    short of stays listed either way.

The rail picks the route or the bodies by itself. It shows the route while another system is your targeted
destination, while the drive is charging to jump and while you are in hyperspace, until the moment you arrive
at the next star: the game clears a route in the middle of its last jump, and the rail keeps showing that jump
until it is made. With nothing targeted, or a body or station in this system targeted, it shows the bodies.
The useful section is up without a click.

**Pop out** moves the rail to a window of its own that stays on top of other windows, for a second monitor or
a stream capture. A one-line strip stays in the main window with the scan progress and these controls:

- **Bring the rail back** docks it again. Closing the rail window does the same.
- **Reset position** puts the rail window back on the primary screen at its default size. Otherwise it
  remembers where you left it and how big you made it.
- **Overlay** makes the window mostly transparent with bright text, so it can sit over a game running in a
  window or borderless. A game in exclusive full screen covers every other window, including this one.
- **Click-through** lets clicks pass through the rail to the game. While it is on, the rail's buttons,
  links and switches hide, since nothing on it can be pressed, and the window's border goes too, since
  nothing on it can be grabbed, so only the rail's own panel shows over the game. Turn click-through off
  from the strip to get them back. It works on Windows only.

## The pages

One section per page.

### Home

The start page: a tile for each section with a line on what it holds, and a panel saying what the plan holds
and whether the journal is being watched. Five figures run across the top:

- **Materials short**: how many materials the plan still lacks.
- **Earned, 7 days**: credits earned in the last week of play.
- **Jumps left**: jumps left on the route, and whether the fuel covers them.
- **Bodies scanned**: this system's scan progress.
- **Data aboard**: what the scans and samples you have not sold yet are worth, counted from your last sale and
  reset by a death. Bodies a nav beacon handed you are not counted: the game pays nothing for data it gave
  away, and the system page prices them at nothing too. When you complete a sample the journal says whether
  anyone had logged that species on that body; when nobody had, Katey counts the first-logged bonus, five
  times the base, as likely. Another commander
  can still sell it first, so the figure is a likelihood, not a promise. Before any sample, the scan's footfall
  flag stands in: life is logged on foot, so a body nobody has set foot on cannot have been logged by anyone,
  and Katey counts the bonus as likely there and not where someone has already landed.

Colour means something everywhere: material grades, the three inventories, the four places a module fits, star
classes as the galaxy map paints them, and the verdicts ready, partial and missing. Have-against-need bars sit
under every material the plan wants, and the fuel bar on the route page and the rail shows the tank with what
the next jump will burn in blue over the orange of what stays, as the game's own gauge draws it.

### Engineers

Every engineer, nearest to you first, with how far away they are and whether they are on your route. Tick
the engineers you have unlocked and intend to use; the ticks drive the plan's warnings and everything the
switch at the top of the page narrows.

**Show only items from selected engineers** is the most important switch in Katey. On, the modules index
and every blueprint, grade and experimental picker offer only what the engineers you ticked can roll, and
each grade picker starts at the grade they reach. Off, everything is offered and grade pickers start at the
top grade. Either way, the plan still warns about an entry nobody you ticked can roll. The switch is
remembered.

A search box filters the list by name, and the columns sort.

### An engineer

One engineer: where they are and how far that is from you, every blueprint they offer with the grade they
stop at and an **Add** button that plans it, which of your plan entries they can roll, how many of your
modules are stored in their system, and whether they are a stop on your route.

### Modules

Every module type that can be engineered, in one group per place on the ship: core internals, optional
internals, hardpoints and utilities. Click a group to open it, or use **Expand all** and **Collapse all**.
With **Show only items from selected engineers** on, a group lists only the types your ticked engineers can
roll a blueprint on. The open groups are remembered.

### A module type

One kind of module, such as the frame shift drive or a beam laser. Its sections:

- **Blueprints.** Each blueprint with the grade your selected engineers reach, everyone who offers it and
  how far away they are, a grade picker and an **Add** button. The pickers follow the engineers switch.
- **Experimentals.** The experimental effects the type takes and what each costs. Grade 0 on a blueprint
  row plans the experimental alone, for a module that already has its blueprint.
- **Variants.** Every size and class the game sells, with the ones you own marked.
- **Owned.** The modules of this type you own, fitted or stored, where they are and how they are
  engineered.
- **Wanted.** What your saved fits ask for of this type.

### A blueprint

One blueprint across every module type it applies to: what one roll costs at each grade, who offers it on
which types, and a bar that adds it to the plan. Pick the module type, the grade and an experimental and
press **Add**, or press **Add** on a grade's row. If your selected engineers cannot reach the blueprint
while the engineers switch is on, the bar says so instead of offering a grade you cannot roll.

### Materials

Every material with how many you hold, how many the plan needs and how many you are short. A switch per
inventory shows or hides raw, manufactured and encoded materials, a name filter narrows the list, and
**Short only** keeps just the ones you lack. All of it is remembered.

Each row also takes a **reserve**: the number you want to keep at least, and a **Trade** switch that says
whether the material may be handed over at a trader at all. The reserves are saved.

Under the table, **At the material traders** says what to hand over to cover everything you are short of and
bring every reserve back up, trade by trade with the rate, grouped by trader. The planner spends only what you
hold above its reserve and never a material whose Trade switch is off. It prefers the trades that lose the
least worth and come from your fullest stacks, and says why when something cannot be covered.

The rates are the game's: six for one going up a grade, one for three going down, and six times as much again
across families. Under each trade a second line names the two materials' families as the traders group them,
such as "Iron family → Carbon family" or "both in the Wake Scans family", followed by how many the whole
batches leave over. Every material named in a trade is a link that lands on its row here. The same list sits
at the bottom of the plan drawer.

### A material

One material: how many you hold against what the plan needs, which plan entries spend it, what else in the
game uses it, blueprint grades and experimentals alike, and which scanned bodies in your current system
carry it.

### Unlocks

Everything the Guardian and the human technology brokers sell, one broker at a time, in collapsible groups:
the Guardian modules, weapons, modified weapons and fighters, and the human broker's modules, weapons and
pre-engineered modules. Click a group to open or close it, or use **Expand all** and **Collapse all**; the
open groups are remembered. Each row shows what the unlock costs in materials, by grade and inventory, and
in commodities you buy at a market.

Tick an unlock and it joins the plan. Its materials count in the plan's totals and in the trades at the
material traders, and its commodities appear under "Buy at a market" in the drawer. Each row's badge says
where it stands:

- **Affordable**, in orange: what the plan leaves in your inventory covers it.
- **Short**, in red: it does not, and the row says by how much. The row is dimmed but can still be ticked;
  it then becomes a short entry like any other, and the drawer says what to collect.
- **Planned**: it is in the plan. Untick it to take it out.
- **Unlocked**, in green: the journal records that you bought it, or you have bought or own a module it
  unlocks, which proves the purchase even when it happened before the journals the game kept. It cannot be
  ticked.

The plan comes first. Ticking one unlock claims its materials, so another that needed the same ones can
turn short; unticking it brings the other back. Below the table, the materials and commodities for every
planned unlock are laid against what you hold.

Some pre-engineered unlocks, such as the engineered FSD V1, are not named in the journal, so Katey cannot
tell whether you have bought them; such a row says so.

### Fleet

Your ships, your saved fits, and the modules you own that no fit uses. Its sections:

- **Ships.** Every ship you own, where it is, and whether it was redeemed from a bundle; see
  [Prebuilt ships](#prebuilt-ships). Cosmetic items such as paint jobs and ship kits are left out
  everywhere in Katey.
- **Fits.** Your saved builds, each with a verdict against everything you own under the sharing policy. That
  is ownership, not one ship's fit; open the fit to compare it with a ship slot by slot.
  - **All owned**: every module the fit wants is somewhere in your fleet or storage.
  - **Partial**: some are owned but not engineered all the way.
  - **Missing**, with how many.

  When more than one owned module would do, the one already fitted to a ship of the fit's hull is named first,
  then one in storage, then one on another ship. Among those in the same place the nearest to you comes first,
  by the coordinates the journal has for its system, with your own system first and a place you have never
  visited last.

  A paste box takes a new fit from EDSY, and a fit can be removed here. A pasted build with the same name as a
  saved fit for the same hull replaces that fit rather than sitting beside it: the button asks first, and a
  second press replaces it in place. The same name on a different hull is a new fit.
- **Unused modules.** The owned modules no fit claims, under a **sharing policy** that says whether modules
  may move between ships, between ships of the same hull only, or not at all, and filters by location,
  place on the ship and engineering state.

The fleet list and the stored-module list are rewritten by the game when you dock somewhere with a shipyard
or outfitting, so they are as fresh as your last such visit.

### A ship

One ship you own: its fitted modules, each with a picker that plans engineering for that very module, and
**Copy for EDSY**, which puts the game's own record of the ship on the clipboard to paste into EDSY's import.

The picker's button says where the job stands:

- **Add to plan** when the choice is a job.
- **In the plan** while the plan already holds the blueprint, grade and experimental chosen for that slot.
- **Already engineered** while the module already has them. Choose a higher grade, another blueprint or
  another experimental and it is a job again.

A module that came with a bundled ship is marked **Prebuilt module**, because engineering it stays with that
ship. For a prebuilt ship the page says whether it is still as it came or how many slots have changed; that
line opens into a table of what each changed slot came with and holds now. See [Prebuilt
ships](#prebuilt-ships).

### A fit

One saved build compared with what you have. Its sections:

- **Against a ship.** The fit slot by slot against one of your ships, starting with the one that already
  matches it best; pick another from the list. Each slot says whether it needs buying, whether it needs
  engineering and whether your selected engineers can do it, and whether a module you already own elsewhere
  would fill the slot instead, within a place and location of your choice.

  **Plan the engineering changes for this ship** appends every engineering difference to the plan; a slot that
  only lacks its experimental becomes an experimental-only entry. The button counts only what the plan does
  not already hold, and stands down, saying so, once every change is planned.

  Each entry is stamped with that ship and slot, as the ship page stamps its own, so the two pages agree on
  what is already planned for that ship. The same change planned for another ship still counts as unplanned
  here. The two buttons on this page recognise each other's entries: a slot's engineering planned as a gap in
  what you own is the same job as the change for this ship, and the ship page counts a fit's entry for its
  hull the same way.
- **Against everything you own.** Each module the fit wants against every module you own, under the fleet's
  sharing policy. A module with the right blueprint at a lower grade, or without the experimental, is
  **partial** rather than missing. So is a module at the right grade that is not yet rolled to its full
  effect; its engineering text says how far along it is, such as "Reinforced G3, 25%", and planning the gap
  asks only for the rolls still owed at that grade. A module engineered before the game kept that figure reads
  as complete.

  The status is coloured by where the module is:

  - green: fitted to a ship of this hull;
  - blue: in storage;
  - yellow: fitted to a ship of another hull;
  - orange: owned but needing a change, or in doubt;
  - red: missing.

  A module that came with a bundled ship cannot leave it, so it counts only for fits of that ship's hull and
  is marked **prebuilt** when it does. **Plan the gaps in what you own** does for this list what the button
  above does for one ship, and stands down the same way. The two counts differ on purpose: a slot can be a
  change for this ship while a module you own elsewhere already covers it.

A module is matched wherever it sits in a slot of the same kind, so a module moved to another optional bay
or hardpoint still counts. **Copy for EDSY** puts the fit on the clipboard in the form EDSY imports: the
export you pasted, exactly as you pasted it, so nothing EDSY wrote is lost on the way back; a fit saved
before exports were kept is rebuilt from its modules instead.

### System

For the system you are in: every body you have scanned, on this visit or an earlier one your journal
history holds, with a **scanned** badge giving the day when it was an earlier visit. When the system lookup
is allowed in Settings, the bodies Spansh knows and you have not scanned join the list with a **from
Spansh** badge: their classes, conditions, signals and known genera, valued like your own, but saying
nothing about discovery, mapping or landing, so no first-mapping or first-logged bonus is assumed for them.
Each body shows with its signals, surface materials, species
predictions and payout estimates, and a line on how many bodies nobody had discovered or mapped before you
and how many you have landed on. Bodies are listed star by star: each star, named by its class and whether
you can scoop it, is followed by the bodies that orbit it, nearest first. Sorting by a column puts the whole
list in that order instead. For a system you visited earlier: where it is and when you were there.

Each body's card and badges call out what matters before you commit to it.

- The card's frame says what the body holds: green for life, blue for surface materials; a body with both
  takes green. A star's card is framed in orange and carries a **star** badge, with **scoopable** in green or
  **not scoopable** in yellow beside it.
- Three status slots, always in the same order, each a yes or no from the flags the game writes with every
  scan: violet for discovery, teal for mapping, green for footfall. A status still open is a solid pill with
  a line beneath: **undiscovered**, **unmapped**, which is where the mapping bonus is, or **no footfall**. A
  status someone has taken is hollow: **discovered**, **mapped**, **footfall**. So a fresh body is three
  solid pills and a worked one three hollow, and you read which is which without reading the words. A star
  has no mapping or footfall slot, and a body nobody can land on has no footfall slot. Your own part, such
  as **Mapped first by you, landed by you.**, is a quiet line under the card. In a populated system footfall
  cannot be claimed and the game's flag says nothing, so Katey treats every body there as trodden. A body
  that came from Spansh says **mapping unknown** and **footfall unknown**.
- Green **landable** badge: a ship can land there.
- Gravity shows beside the distance. It turns yellow from 1.0 g with a **high gravity** badge, and red from
  1.5 g with **extreme gravity**: at that pull a heavy ship may not be able to stop its descent, so approach
  slowly and level early.

**Copy name** puts the system's name on the clipboard for the galaxy map. Every system name anywhere in Katey
is a link to this page.

The species predictions come from where each species has been found before. Before a body is mapped, a genus
signal is matched against its atmosphere, type, volcanism, gravity, temperature and pressure; once mapped, the
scanner's own genus list is used.

Until then the body's exobiology figure is a prediction, marked "likely":

- each genus that can live under the body's conditions is weighted by how often it has been found under such
  conditions;
- that weighted payout is multiplied by the number of biological signals;
- a line says how many genera could live there and names the three likeliest, each with its odds, its
  likeliest species and its expected payout before any bonus. The signals may come from any genus the
  conditions allow, not only those three.

The signal count shows as a badge. A species nobody has logged on a body pays five times its base value. The
journal cannot say whether anyone has, so when nobody had mapped the body Katey takes the bonus as likely and
shows the figure times five, with the base beside it. The rail ranks bodies by that figure, known or
predicted, bonus included, then by signal count. The system page's **Landable** column sorts the same way with
the landable bodies first.

### Route

Two sections:

- **The route** the game has plotted, star by star, with where you are first in each row. Each stop says
  whether it is **new to you**, **visited**, or **surveyed** by a honk or a nav beacon, the distance from the
  start, and the fuel expected on arrival.
  - The system you have targeted is always the next jump. Targeted without a route, it is a one-jump route.
    Targeted further along the route, the stops between drop out. Targeted off the route, the page shows the
    one jump to it and a line saying where the plotted route resumes and how many jumps remain. The target's
    distance comes from your history or the Spansh plot, and reads as 0 when neither knows the system.
  - The fuel projection runs the game's tank reading along the legs ahead with the ship's own burn, by the
    game's own equation from the masses, cargo and drive figures the journal reports. It is there from the
    first moment and says how many jumps are left before you must scoop, which scoopable star to scoop by, and
    what each stop costs. Each jump is costed at the fuel left for it, so the ship lightening as it burns is
    followed.
  - The cargo is the tonnes aboard now, and the projection is redone on every journal update, so a sale or a
    pickup on the way changes it the moment the game reports it. A ship whose loadout the journal has not
    given the masses for falls back to the burn learned from its recent jumps. A departure from a neutron star
    or a white dwarf counts as a boosted jump, and a Guardian booster as extra range.
  - The tank bar above the stops is drawn as the game's own gauge is: orange is what stays in the tank after
    the next jump, blue is what that jump burns, and the blue figure beside the reading is the same burn in
    tonnes.
  - Once you have jumped, the game takes the fuel at once and the tank shows it, so during the jump the stop
    you are arriving at shows the tank reading and the blue bar moves to the jump after.
- **The Spansh plot.** With the plotter turned on in [Settings](#settings), this asks Spansh's galaxy plotter
  for a route from where you are to any system, for a ship you own or a saved fit, from its own masses, tanks
  and drive figures: the journal's for a ship, the EDSY export's for a fit. Neutron boosts, FSD injections,
  secondary star scoops and refuelling at every scoopable star are options.
  - The picker starts on the ship you are flying. Clicking it opens a panel with an empty search box and the
    list: the ship or fit you had chosen first, the ship you are flying second, then the saved fits for the
    chosen hull, your ships and the other fits, each with its full-tank range, the ship you are flying in the
    accent colour. Type to narrow the list by name or hull; click an entry, or press Enter for the first one,
    to choose it. Nothing else changes the choice.
  - A fit saved before its export was kept has no masses to plot with, so it is left out and a note says to
    paste it again from EDSY.
  - The plotter is told the drive is supercharged when you have taken a jet-cone boost since your last jump,
    or when you sit at a neutron star with neutron boosts switched on, so the first jump is planned as a
    boosted one.
  - Katey's drive figures are the same tables Spansh's own site works from, SCO and Mk II drives included,
    so a plan asked for through Katey matches one made by pasting the build on the site.
  - The destination, the routing and the options are saved as you change them. The ship or fit and the cargo
    stay as you set them while the plot is held, across pages and restarts, until you press **Forget the
    plot**; then the page opens again on the ship you are flying, with the cargo it carries filled in, which
    you can type over. Switching ships or fits leaves the cargo alone.
  - The plot lists every waypoint with its jump, the distance left, the fuel the plotter expects on arrival
    and whether it is a neutron star or a refuel stop. It follows your jumps to say which waypoint is next,
    copies that name for the galaxy map, and is kept between runs.
  - Leave the plot for a detour, to scoop say, and the last waypoint you reached since the plot was made is
    marked **last known** while the one after it stays the next waypoint, so **Copy** still gives you the
    neutron star to aim for. A plot made from somewhere you are not has no last known waypoint until you
    reach one of its stops, even if you passed through some of them before.

### Stats

Your journal history read as a ledger. Its sections:

- **Income.** A ring chart of what you have earned by activity, from bounties to exobiology, with a switch
  per activity that redraws the chart without it, and a period picker from all time down to the last week.
- **Exobiology.** What is aboard and unsold: samples, how many are likely first-logged, and their worth with
  and without the bonus. Then what organic data earned with and without the first-logged bonus, and every
  species sold.
- **Exploration.** What is aboard and unsold: systems, bodies scanned and mapped, and their estimated
  cartographics value. Then the period's sales next to the game's lifetime figures.
- **Combat, trading and mining, missions, Powerplay.** Each activity's own figures for the period, next to
  the game's lifetime figures for it.
- **Wings and crews.** Time flown with others, what the game marked as shared by kind, every commander you
  have flown with, with the time together and what was shared while they were present, and every wing
  mission accepted, with who was in the wing at the time and how it stands. Flying in a wing while you
  explore alone shares nothing, so it counts for nothing here. The journal does not say whether a wing
  mission came from the board or from a wingmate, so both are listed.
- **Lifetime figures.** Every running total the game reports, time played included. A remembered switch
  also lists the figures at zero; it is off by default. The period picker hides here, since these are the
  game's own totals.

### Settings

- **Journal folder.** The folder Katey watches. **Detect** looks in the usual places, **Browse** picks one,
  **Watch** starts reading it and **Stop** stops. The status line says what the watch is doing.
- **Journal backup.** A folder the whole journal folder is copied into, only what is new or changed, when
  Katey closes and whenever you press **Back up now**. Point it at a folder OneDrive or another cloud
  drive syncs and your journals leave the machine with it.
- **Saved data.** The folder Katey keeps its own data in; see [Your data](#your-data). Choose another and
  the data moves there at once. If the folder already holds a Katey data file it is loaded, otherwise the
  current data is written there, and the data at the old place is left as it was. Two machines pointed at
  one synced folder share one plan, one set of engineers and one set of fits.
- **Outgoing connections.** The one place that decides what Katey may send over the network, one switch
  per kind of request, every one off until you tick it. There are two kinds, both to
  [Spansh](https://spansh.co.uk/): asking for a route when you press **Plot**, and asking about the bodies
  of the system you have just arrived in. The switches are remembered.

## Working with EDSY

[EDSY](https://edsy.org/) is the ship builder most of the community uses, and Katey speaks its import and
export forms in both directions.

- **A build into the plan.** In EDSY, open the build and choose **Export**, then the journal-style Loadout
  text. Paste it into the plan drawer's import box and press **Read**. Every engineered module becomes an
  entry at its blueprint, grade and experimental; untick the ones you do not want and press **Add**.
- **A build as a saved fit.** Paste the same text into the saved fits box on the [Fleet](#fleet) page.
  The fit is then compared against your ships and your owned modules. Pasting a newer export of a build
  you already saved, same name and same hull, replaces the saved fit after you confirm.
- **A ship or a fit back to EDSY.** **Copy for EDSY** on a [ship](#a-ship) or a [fit](#a-fit) page puts
  a Loadout on the clipboard. In EDSY choose **Import** and paste.

## Prebuilt ships

A ship redeemed from a bundle in the game's store comes with modules Katey marks as prebuilt for as long
as they stay in the slots they came in. Buying, retrieving, swapping, storing or selling into a slot ends
the mark, whatever the game shows for the newcomer's value, as does a bought module of the same kind
taking the slot. The game's bundle modules cannot be stored, moved to another ship or sold. Engineering
such a module is engineering that stays with that ship. Katey marks these:

- The [Fleet](#fleet) page marks a redeemed ship as **prebuilt**.
- A [ship](#a-ship) page marks each such module **Prebuilt module** beside its engineering picker, and a
  [fit](#a-fit) comparison warns where the slot to engineer holds one.
- A ship page also says whether a prebuilt ship is **still as it came**, so selling it and redeeming it
  again loses nothing, or how many slots have changed since. The line opens into a table of what each
  changed slot came with and holds now. A prebuilt module swapped for a bought one of the same kind counts
  as a change, since the bought one can leave the ship.

A prebuilt ship you have never deployed does not exist to the game, so it cannot appear in the fleet.

## What the journal cannot tell Katey

Katey knows only what the game writes. A few limits follow:

- Whether someone else has discovered a system is only known once you scan a body in it. Ahead of arrival,
  Katey can only say whether *you* have been there.
- The journal does not record who first discovered or mapped a body, or who first landed on it, only
  whether you were first.
- The fleet list and the stored-module list are rewritten when you dock somewhere with a shipyard or
  outfitting, so they are as fresh as your last such visit. A stored module's blueprint and grade are
  recorded but not its experimental effect, which the fit checker reports as uncertain.
- The journal does not say whether a wing mission came from the mission board or from a wingmate.
- Bodies are shown for the system you are in; an earlier system shows where it is and when you were there.
- A body you scanned on an earlier visit is already in your data, so the game writes nothing when the honk
  finds it again. Katey remembers every body you scanned on any visit within your journal history and lists it
  again when you return, marked with the day it was scanned. A body scanned before your oldest journal file
  cannot be recovered that way: when the honk reports the whole system already known, Katey counts every body
  as scanned but can list only the ones it has, or the ones Spansh knows when that switch is on.
- The bodies marked **from Spansh** are what Spansh knows, which can be older than the system is: a survey
  from years ago, or only part of a system. Katey asks about a system once, when you arrive in it, and keeps
  the answer for the run; if Spansh cannot be reached it waits five minutes before asking about that system
  again.

## Your data

Katey keeps one file: the plan, the engineer selection, the saved fits, the material reserves, the journal
folder, the backup folder and switch, the outgoing connections switches and the last plot, the rail's placement and
settings, and every remembered view choice. By default it lives under your local application data folder,
in `Katey\state.json`. The **Saved data** box in [Settings](#settings) moves it anywhere, a synced folder
included.

Katey reads the journal files locally and sends nothing anywhere unless you allow it under **Outgoing
connections** in [Settings](#settings), where everything is off to begin with. Two kinds of request exist,
both to [Spansh](https://spansh.co.uk/): when you press **Plot**, two system names and the ship's jump
figures; and, on arriving in a system, that system's id, which brings back the bodies Spansh knows there.
Every request names Katey, its version and the project's address, as Spansh asks of tools that use its
service. The journal backup copies files only to the folder you choose.

If the day's journal file goes missing while the game runs, the game writes it again from the start, and
everything it held is lost to the game and to Katey. The backup keeps its earlier copy of such a file beside
the new one, named with a `rewritten` suffix. Until your next jump Katey takes your position from the game's
route file, whose first stop is the system you are in, so the rail and the route page stay right; the system's
bodies are empty until you scan again.
