# Katey.Ui

The desktop window, built with Avalonia. It is a thin shell over [`Katey.App`](../Katey.App/README.md):
the XAML binds to the view models there, and the code-behind holds only what needs the platform.

## Running it

```sh
dotnet run --project src/Katey.Ui
```

The window opens on the start page and starts watching the saved journal folder, or the detected one.

## The look

Two surfaces, two palettes. The workbench follows the system theme, light or dark, and reads like a
chart table: cool neutrals, a condensed display face for headings and labels, a monospaced face wherever
figures line up. The live rail is always dark, whatever the workbench does, because it is the cockpit and
it has to sit comfortably over the game when it is in its own window. Its text is the HUD's orange: body text
in a readable amber, secondary lines in a dimmer orange, and headings, the live mark and links in a pale
bright orange, so the rail reads like the cockpit panels it sits beside. The system name alone is warm
white, so the one thing you look for first stands off the orange. Warnings stay gold, good news green and
trouble red.

The identity colour is the game's cockpit orange, deepened in the light theme so white text reads on it.
It carries the wordmark, links, the lit navbar entry, primary buttons and the rail's live mark and
headings. Every other colour means something, and the meanings live as resource pairs in `App.axaml`:

- **Material grades** `KG1` to `KG5` climb from grey through green, blue and violet to gold. A grade
  figure, a chip's edge and a grade badge take the grade's colour.
- **Inventories** `KRaw`, `KManufactured` and `KEncoded` are copper, steel and teal: the swatch on a
  chip and on a materials section heading.
- **Module places** `KCore`, `KOptional`, `KHardpoint` and `KUtility` are gold, blue, red and green: the
  swatch beside a slot name on the ship and fit pages, and the strip on a modules-index tile.
- **Star classes** `KStarO` to `KStarM`, plus neutron, white dwarf, black hole and other, are the galaxy
  map's colours. They are fixed, not themed, and paint the star dot on the route page and the rail.
- **Verdicts** use the three status colours: a `Border.badge` is `good`, `warn` or `bad`, so ready, partial
  and missing read at a glance, as do flying and prebuilt. A fit line's status badge adds two: `burn`,
  blue, for a module in storage, and `accent`, orange, for one that needs a change, so where a module is
  reads at a glance: green here, blue stored, yellow on another ship. A body's gravity badge is `warn` for
  high and `bad` for extreme, with the gravity figure itself coloured to match, and `good` marks landable.
  A body card's frame says what it holds: `Border.panel.minerals` is blue for surface
  materials, `Border.panel.biology` green for life, which wins over materials, and `Border.panel.star` the
  accent for a star. The rail's `Border.tag` does the same in its fixed palette.
- **Exploration statuses** `KDiscovery`, `KMapping` and `KFootfall` are violet, teal and green: a body
  card's three status slots, `Border.badge.status` with `.discovery`, `.mapping` or `.footfall`, one per
  flag the game writes with a scan. The `open` class fills the pill in its colour and thickens its bottom
  edge; without it the pill is hollow with grey text, so open against taken reads by shape, not by
  brightness.
- **The next jump's burn** `KBurn` is the HUD's blue: the part of the fuel bar the next jump will take,
  and the figure beside the reading that says the same in tonnes.

The view reaches a meaning colour through `SemanticBrush`, whose converter parameter is the key prefix and
whose value is the suffix, so a grade of 3 with `KG` finds `KG3`. The brush is the one in force when the
binding evaluates; a page rebuilt after a theme change picks up the other theme. `StarBrush` does the same
for a star class string. Figures that matter get room: `TextBlock.stat` is the big number on a start-page
tile, `ProgressBar.meter` the thin bar under a have-against-need figure, and the rail's scan ring is an
`Arc` driven by the scanned fraction through `Scale`. The fuel tank is two meters in one place, as the
game's own gauge draws it: a blue `burn` meter to the tank's level under an `over` meter with no track to
the level after the next jump, so the blue that shows is what the jump burns.

The fonts ship inside the executable. B612, the typeface Airbus commissioned for cockpit displays, sets
the body text; B612 Mono sets every figure that lines up in a column; Barlow Semi Condensed sets headings
and labels. All three are under the SIL Open Font Licence; see [NOTICE.md](../../NOTICE.md).

## The window frame

The main window and the rail window ask for a border without a title bar, `WindowDecorations="BorderOnly"`,
and draw the bar themselves: the main window's top bar and the rail window's strip. Each carries its own
minimise, maximise and close buttons in the app's colours. Dragging the bar moves the window and
double-clicking it maximises, through `WindowDrag`, which leaves presses on buttons and text boxes to
them. The system still provides the resize borders. The rail window drops its border and its resizing
while clicks fall through it, since nothing on it can be grabbed then. Both windows carry Katey's face as
their icon, drawn in code from `Assets/katey.ico`, which is also the executable's icon.

## The rail's two homes

`RailView` is one user control over `RailViewModel`. The main window shows it docked on the right. "Pop
out" sets `IsDetached`, and the main window reacts by opening `RailWindow`, an always-on-top window over
the same view model, and collapsing the docked column to a one-line strip with the four numbers you glance
at most and a "Bring the rail back" button. Closing the rail window, or pressing that button, docks it
again. Where the rail was is saved, so it comes back the same way next time. The rail paints its dark
ground only as far as its content reaches, with the window behind it painting the rest, so in overlay mode
everything below the last line is clear and the game shows through untinted there.

## File map

| File | Purpose |
| --- | --- |
| `Log.cs` | Source-generated log messages for the host. |
| `Program.cs` | Entry point and Avalonia configuration. Opens `katey.log` beside the default saved data, which every logger writes to at Information and above, or at the floor `--log-level <level>` or `--verbose` names, every line scrubbed of folder paths and of the account and machine names; writes an exception that ends the process there before it dies. |
| `SingleInstance.cs` | One Katey per user: a named mutex claimed at start, and the notice a second copy shows instead of a main window. |
| `App.axaml`, `App.axaml.cs` | The theme palettes with the meaning colours, the rail palette, the star colours, the shared converters, the font resources, the style include; builds the catalogs, the predictor, the state store, the trackers, the journal monitor, the HTTP client with the Spansh plotter, the journal backup and the `ShellViewModel`; on exit runs the shell's shutdown, which backs the journals up when asked, then disposes the monitor and the client. |
| `Styles/Workbench.axaml` | Every style: text roles, panels, chips, grade badges, buttons including the link style, and the rail's fixed-palette styles. |
| `MainWindow.axaml` | The top bar with the wordmark, the navbar, the search box, the plan button and the caption buttons; one data template per page kind; the docked rail and its retracted strip; the search results; and the plan drawer. |
| `MainWindow.axaml.cs` | Places the window on the primary screen and starts the watch when it opens, opens and closes the rail window, records and restores each page's scroll position around navigation, the caption buttons and the title-bar drag, the folder pickers for the journal, the backup and the saved data, the clipboard for the shopping list, the EDSY copies, the plot's waypoints and a system's name, the search box's Enter, Escape and Ctrl K, and the plot's ship-or-fit picker, a button whose flyout holds a search box and a list: opening it empties and focuses the box, a click on an entry or Enter chooses and closes, and nothing else changes the choice. |
| `Converters.cs` | `BrushFromHex` and `GeometryFromPath`, which let the statistics page's chart bind colours and sector outlines from plain strings; `SemanticBrush` and `StarBrush`, which look a grade, an inventory, a module place or a star class up in the palette; and `Scale`, which turns a fraction into an angle or a width. |
| `WindowDrag.cs` | Moves a window by its own title bar and maximises on a double click. |
| `ClipboardText.cs` | Puts text on the clipboard for whichever window a control is in. |
| `PageHeaderView.axaml` | A page's breadcrumb links, kicker and title. |
| `SortHeaderView.axaml` | A table column heading that sorts on click, with the direction mark. |
| `RailView.axaml`, `RailView.axaml.cs` | The rail, used docked and detached; copies the next Spansh waypoint. |
| `RailWindow.axaml`, `RailWindow.axaml.cs` | The rail's own window, with a drag strip that carries the live mark, and a close button. Closing it docks the rail. It remembers its place and size through the view model, applying them after the decorations because a frame change rewrites the size, becomes an overlay with a mostly transparent ground, and lets clicks through with a Win32 extended style, dropping the system's resize border while it does. |
| `Assets/Fonts/` | The bundled font files and their licence. |
| `Assets/katey.ico` | The icon. |
