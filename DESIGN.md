---
version: alpha
name: UGSGit
description: >-
  Desktop Git GUI client (fork of SourceGit) with a minimal, dense,
  developer-focused aesthetic. Built on Avalonia UI with a dual light/dark
  theme system. Visual identity prioritizes information density, muted
  surface tones, and crisp monospace diff rendering over decorative elements.

colors:
  # ── System (dynamic / OS-provided) ──────────────────────────────────────
  accent: "dynamic: SystemAccentColor"
  accent-hovered: "dynamic: SystemListLowColor"
  dark-orange: "#FFFF8C00"
  commit-link: "#FFFF8C00"

  # ── Light theme ─────────────────────────────────────────────────────────
  # Surfaces
  window-light: "#FFF0F5F9"
  window-border-light: "#FF999999"
  titlebar-light: "#FFCFDEEA"
  toolbar-light: "#FFF0F5F9"
  popup-light: "#FFF8F8F8"
  contents-light: "#FFFAFAFA"
  datagrid-header-light: "#FFF0F5F9"

  # Text
  fg1-light: "#FF1F1F1F"
  fg2-light: "#FF6F6F6F"

  # Borders
  border-subtle-light: "#FFCFCFCF"
  border-interactive-light: "#FF898989"
  border-separator-light: "#FFCFCFCF"

  # Badges & highlights
  badge-light: "#FFB0CEE8"
  badge-fg-light: "#FF1F1F1F"
  link-light: "#0000EE"
  conflict-light: "#FF836C2E"
  conflict-fg-light: "#FFFFFFFF"
  conflict-mine-light: "#400078D7"
  conflict-theirs-light: "#40FF8C00"
  inline-code-light: "#FFE4E4E4"
  inline-code-fg-light: "#FF000000"

  # Flat button
  flat-button-bg-light: "#FFF8F8F8"
  flat-button-bg-hovered-light: "#FFFFFFFF"
  flat-button-border-light: "#FF898989"

  # Diff
  diff-empty-light: "#10000000"
  diff-added-bg-light: "#80BFE6C1"
  diff-deleted-bg-light: "#80FF9797"
  diff-added-hl-light: "#A7E1A7"
  diff-deleted-hl-light: "#F19B9D"
  diff-block-border-light: "DarkCyan"

  # ── Dark theme (default) ────────────────────────────────────────────────
  # Surfaces
  window: "#FF252525"
  window-border: "#FF606060"
  titlebar: "#FF1F1F1F"
  toolbar: "#FF2F2F2F"
  popup: "#FF2B2B2B"
  contents: "#FF1C1C1C"
  datagrid-header: "#FF2B2B2B"

  # Text
  fg1: "#FFDDDDDD"
  fg2: "#40F1F1F1"

  # Borders
  border-subtle: "#FF181818"
  border-interactive: "#FF7C7C7C"
  border-separator: "#FF404040"

  # Badges & highlights
  badge: "#FF8F8F8F"
  badge-fg: "#FFDDDDDD"
  link: "#4DAAFC"
  conflict: "#FFFAFAD2"
  conflict-fg: "#FF252525"
  conflict-mine: "#400078D7"
  conflict-theirs: "#40FF8C00"
  inline-code: "#FF383838"
  inline-code-fg: "#FFF0F0F0"

  # Flat button
  flat-button-bg: "#FF303030"
  flat-button-bg-hovered: "#FF333333"
  flat-button-border: "#FF4F4F4F"

  # Diff
  diff-empty: "#3C000000"
  diff-added-bg: "#C03A5C3F"
  diff-deleted-bg: "#C0633F3E"
  diff-added-hl: "#A0308D3C"
  diff-deleted-hl: "#A09F4247"
  diff-block-border: "DarkCyan"

typography:
  body:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: 400
    lineHeight: 1.4
  body-small:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: 400
  body-bold:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: 700
  tab-header:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: 700
  label:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: 700
  group-header:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: 700
  info-label:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: 700
  menu-item-shortcut:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: 400
  commit-link:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: 400
  code:
    fontFamily: "fonts:UGSGit#JetBrains Mono"
    fontSize: 13px
    fontWeight: 400
  inline-code:
    fontFamily: "fonts:UGSGit#JetBrains Mono"
    fontSize: 13px
    fontWeight: 400

rounded:
  none: 0px
  xs: 3px
  sm: 2px
  md: 4px
  lg: 8px
  xl: 12px
  pill: 14px
  full: 16px
  window: 8px

spacing:
  base: 8px
  xs: 2px
  sm: 4px
  md: 8px
  lg: 12px
  xl: 16px
  2xl: 24px
  3xl: 48px
  menu-item-icon: 28px
  tab-item-padding: 10px
  list-item-height: 24px
  scrollbar-width: 8px
  resize-border: 12px
  window-padding: 12px
  tooltip-padding: 8px

components:
  window-frameless:
    padding: "{spacing.resize-border}"
    rounded: "{rounded.window}"
    backgroundColor: transparent
    dropShadow: "0 0 12 #60000000"
  window-frameless-maximized:
    padding: 0px
    rounded: 0px
  tooltip:
    backgroundColor: "{colors.popup}"
    textColor: "{colors.fg1}"
    borderColor: "{colors.border-separator}"
    rounded: "{rounded.md}"
    padding: "4px,6px"
    dropShadow: "0 0 8 #30000000"
  flyout:
    backgroundColor: "{colors.popup}"
    rounded: "{rounded.md}"
    padding: "{spacing.lg}"
    dropShadow: "0 0 4 #A0000000"
  context-menu:
    backgroundColor: "{colors.popup}"
    rounded: 0px
    padding: "{spacing.md},{spacing.sm}"
    dropShadow: "0 0 4 #A0000000"
  menu-item:
    height: 28px
    rounded: "{rounded.xs}"
    iconColumnWidth: 28px
  menu-separator:
    margin: "28px,0,4px,0"
  scrollbar-vertical:
    width: 8px
  scrollbar-horizontal:
    height: 8px
  scrollbar-thumb:
    rounded: "{rounded.md}"
  text-input:
    rounded: 0px
    padding: "4px,0"
    minHeight: 16px
    borderColor: "{colors.border-interactive}"
    backgroundColor: "{colors.contents}"
    textColor: "{colors.fg1}"
  text-input-hover:
    borderColor: "{colors.accent}"
  text-input-focus:
    borderColor: "{colors.accent}"
    borderWidth: 2px
  caption-button:
    width: 48px
    backgroundColor: transparent
    rounded: 0px
  caption-button-hover:
    backgroundColor: "#40000000"
  caption-button-close-hover:
    backgroundColor: Red
  icon-button:
    backgroundColor: transparent
    opacity: 0.8
  icon-button-hover:
    opacity: 1
  icon-button-disabled:
    textColor: "{colors.fg2}"
  flat-button:
    borderWidth: 1px
    borderColor: "{colors.border-separator}"
    backgroundColor: "{colors.flat-button-bg}"
    textColor: "{colors.fg1}"
    fontWeight: 700
  flat-button-hover:
    backgroundColor: "{colors.flat-button-bg-hovered}"
  flat-button-primary:
    textColor: "{colors.accent}"
    borderColor: "{colors.accent}"
    backgroundColor: "{colors.accent}"
  flat-button-primary-hover:
    backgroundColor: "{colors.accent-hovered}"
  list-item:
    height: "{spacing.list-item-height}"
    padding: 0
  list-item-hover:
    backgroundColor: "{colors.accent-hovered}"
    opacity: 0.5
  list-item-selected:
    backgroundColor: "{colors.accent-hovered}"
    opacity: 1
  checkbox:
    width: 16px
    height: 16px
    rounded: "{rounded.sm}"
  checkbox-hover:
    borderColor: "{colors.accent}"
  checkbox-checked:
    accentFill: "{colors.accent}"
  checkbox-focus:
    borderColor: "{colors.accent}"
    borderWidth: 2px
  radioButton:
    width: 14px
    height: 14px
    innerDotSize: 10x10px
  switch-button:
    height: 24px
    rounded: "{rounded.pill}"
  comboBox:
    minHeight: 20px
    borderColor: "{colors.border-interactive}"
    itemRounded: "{rounded.sm}"
  tab-item:
    fontSize: 14px
    padding: "10px,0"
    minHeight: 24px
  completion-list:
    backgroundColor: "{colors.popup}"
    borderColor: "{colors.border-subtle}"
    padding: "{spacing.md},{spacing.sm}"
    rounded: "{rounded.lg}"
    itemHeight: 24px
    itemRounded: "{rounded.lg}"
---

## Overview

SourceGit is a **dense, information-first Git GUI** that prioritizes signal over decoration. The design language draws from professional developer tools — compact toolbars, muted surface palettes, and a strong reliance on iconographic path indicators for state communication.

The visual identity is characterized by:

- **Tonal restraint.** Both themes use closely spaced surface values. The dark theme ranges from `#1C1C1C` (content areas) to `#2F2F2F` (toolbars), creating depth through subtle tonal shifts rather than heavy shadows.
- **Accent delegation.** The system accent color (derived from the OS) is reserved exclusively for interactive states — focus rings, selection highlights, primary buttons — never for decoration.
- **Monospace integrity.** Commit hashes, diffs, and code content render in JetBrains Mono, clearly separating data from UI chrome.
- **Zero-radius edges.** Text inputs, separators, and most containers use `CornerRadius=0`. Rounded corners appear only at the window level (8px), tooltips (4px), and small interactive elements like badges and switches.

## Colors

### Dark theme (default)

The dark palette is the primary theme. It employs a **three-tier surface system**:

- **Content layer (`#1C1C1C`):** The deepest surface, used for editing areas and the main content background. This is where the user's work lives.
- **Window/chrome layer (`#252525`):** The primary window background. Toolbars at `#2F2F2F` and popups at `#2B2B2B` sit slightly above the content, providing gentle layer discrimination.
- **Titlebar (`#1F1F1F`):** The most recessed surface, visually anchoring the window controls.

**Text** uses two levels. `FG1` (`colors.fg1`: `#DDDDDD`) is the primary text color across all surfaces. `FG2` (`colors.fg2`: `#40F1F1F1`) is a 25%-opacity white for secondary labels, metadata, and inactive states in dark mode; a solid `#6F6F6F` gray in light mode.

**Borders** follow a similar three-tier pattern: `border-subtle` (`#181818`) for structural borders, `border-separator` (`#404040`) for dividers within content, and `border-interactive` (`#7C7C7C`) for interactive borders (text inputs, combo boxes).

**Accent colors** (`colors.accent`, `colors.accent-hovered`) are OS-provided dynamic resources (`SystemAccentColor`, `SystemListLowColor`). They are never hardcoded — the application delegates to the platform for these values.

**Diff colors** use translucent fills at two opacity levels — background washes and line-level highlights — ensuring diff hunks are distinguishable without overwhelming the code content.

### Light theme

The light theme mirrors the dark structure with inverted luminance:

- **Content layer (`#FAFAFA`):** Near-white surface for editing areas.
- **Window/chrome layer (`#F0F5F9`):** A cool blue-gray for toolbars and window backgrounds.
- **Titlebar (`#CFDEEA`):** A stronger blue-gray providing visual framing.

Primary text is `#1F1F1F`, secondary text is `#6F6F6F`. Interactive borders use `#898989`, while light separators use `#CFCFCF`.

**Links** use classic blue (`#0000EE`) in light mode and a softer blue (`#4DAAFC`) in dark mode. **Commit references** (`color.dark-orange`) are rendered in `DarkOrange` in both themes, used for commit SHAs in dialogs, blame annotations, and signature verification.

### Token mapping (upstream XAML keys → DESIGN.md tokens)

| Upstream XAML key | DESIGN.md token (dark) | DESIGN.md token (light) |
|---|---|---|
| `Color.Window` | `window` | `window-light` |
| `Color.WindowBorder` | `window-border` | `window-border-light` |
| `Color.TitleBar` | `titlebar` | `titlebar-light` |
| `Color.ToolBar` | `toolbar` | `toolbar-light` |
| `Color.Popup` | `popup` | `popup-light` |
| `Color.Contents` | `contents` | `contents-light` |
| `Color.DataGridHeaderBG` | `datagrid-header` | `datagrid-header-light` |
| `Color.FG1` | `fg1` | `fg1-light` |
| `Color.FG2` | `fg2` | `fg2-light` |
| `Color.Border0` | `border-subtle` | `border-subtle-light` |
| `Color.Border1` | `border-interactive` | `border-interactive-light` |
| `Color.Border2` | `border-separator` | `border-separator-light` |
| `Brush.Accent` | `accent` (dynamic: `SystemAccentColor`) |
| `Brush.AccentHovered` | `accent-hovered` (dynamic: `SystemListLowColor`) |
| `Color.Badge` | `badge` | `badge-light` |
| `Color.BadgeFG` | `badge-fg` | `badge-fg-light` |
| `Color.Link` | `link` | `link-light` |
| `Color.Conflict` | `conflict` | `conflict-light` |
| `Color.Conflict.Foreground` | `conflict-fg` | `conflict-fg-light` |
| `Color.InlineCode` | `inline-code` | `inline-code-light` |
| `Color.InlineCodeFG` | `inline-code-fg` | `inline-code-fg-light` |
| `Color.FlatButton.Background` | `flat-button-bg` | `flat-button-bg-light` |
| `Color.FlatButton.BackgroundHovered` | `flat-button-bg-hovered` | `flat-button-bg-hovered-light` |
| `Color.FlatButton.FloatingBorder` | `flat-button-border` | `flat-button-border-light` |
| *DarkOrange (hardcoded)* | `dark-orange` / `commit-link` | `dark-orange-light` |

Colors with no DESIGN.md token (used directly in XAML): `Red` (error text, close button hover).

## Typography

The type system uses two font families with clearly separated roles:

- **Inter** handles all UI text. It is a humanist sans-serif optimized for screen legibility at small sizes, with a tall x-height and open apertures that remain readable at the default 13px. In Avalonia, the font is loaded via the font-pack URI `fonts:Inter#Inter`.
- **JetBrains Mono** handles all monospace content — commit hashes, diffs, inline code, and code editing. Its distinctive characters (e.g., `0` with a dot, `1` with a serif) reduce ambiguity in data-dense views. In Avalonia, the font is loaded via the font-pack URI `fonts:UGSGit#JetBrains Mono`.

Text styles maintain a strict hierarchy through `FontWeight` rather than size variation:

| Role | Weight | Size | Notes |
|---|---|---|---|
| Tab headers | Bold | 14px | Opacity 0.56 when inactive, 1.0 when selected |
| Group headers | Bold | 13px | Secondary color (`FG2`) |
| Info labels | Bold | 13px | Right-aligned, secondary color |
| Body | Regular | 13px | Default for all controls |
| Body small | Regular | 11px | Decreased from default via converter |
| Menu shortcuts | Regular | 11px | Right-aligned, secondary foreground |
| Commit links | Regular + Underline | — | `#DarkOrange` color |

Global font size is user-configurable (default 13px) with a zoom factor (0.05 step, range 1.0–2.5) applied via `LayoutTransformControl`.

## Layout

The layout follows a **sidebar + content** model with a compact, fixed-height toolbar zone:

- **Launcher window:** Minimum 1024×600. Title bar (30px) with icon buttons, workspace selector, and tab bar. Content area fills remaining space.
- **Repository view:** Left sidebar (branches, tags, remotes, submodules, worktrees) + main content (histories graph + working copy + diff).
- **Custom window frame:** 12px invisible resize borders on all edges (hidden when maximized). Window chrome collapses entirely when maximized.
- **Inner panels** use 8px as the fundamental spacing unit, with 4px for tight groupings and 12–16px for section dividers.
- **Menu items** follow a 28px row height with a 28px icon column — maintaining pixel-precise alignment.
- **Scrollbars** auto-hide after 200ms (configurable to static). 8px wide, with 24px minimum thumb height, expanding to fill track on hover.

The command palette overlay uses a 420px-wide centered popup with 8px corner radius and a deep drop shadow (`0 0 12 #A0000000`).

## Elevation & Depth

SourceGit uses **tonal layering** rather than shadow-based elevation:

1. **Surface differentiation** via color value increments of 8–12 RGB steps (dark: `#1C` → `#25` → `#2B` → `#2F`; light: `#FA` → `#F0` → `#CF`).
2. **Drop shadows** are reserved exclusively for floating chromeless elements:
   - Tooltips: `0 0 8 #30000000`
   - Context menus and flyouts: `0 0 4 #A0000000`
   - Custom window frame: `0 0 12 #60000000`
   - Command palette: `0 0 12 #A0000000`
3. **No inner shadows, no gradient fills.** Flat color fills throughout.

Interactive elevation is communicated through **opacity modulation** (icon buttons: 0.8 → 1.0 on hover; tabs: 0.56 → 1.0 on selection) and **accent color** highlights (border color change on focus, background tint on selection).

## Shapes

The shape language is **functionally flat** with targeted rounding:

- **Windows:** 8px corner radius in normal state, 0px when maximized.
- **Tooltips and flyouts:** 4px corners.
- **Badges and chips:** Pill-shaped (8–16px radius depending on size).
- **Context menu items:** 3px corners.
- **Text inputs:** 0px corners (sharp, no rounding).
- **CheckBoxes:** 2px corners.
- **ComboBox items:** 3px corners.
- **Completion list:** 4px outer, 4px per item.
- **Scroll thumbs:** 4px corners.

The deliberate mix — rounded windows and popups, sharp inputs and separators — creates a clear visual distinction between **containers** (rounded) and **inline controls** (sharp).

## Components

### Buttons

Three button tiers, each with strict visual hierarchy:

- **Primary (accent-filled):** Uses system accent colors (`colors.accent`) via `AccentButtonBackground` / `AccentButtonForeground` Avalonia resources. Bold weight (`700`). Border 1px. The single most prominent action per view.
- **Flat (outlined):** Border 1px (`colors.border-separator`), bold weight (`700`), fills to `flat-button-bg-hovered` on hover. Used for secondary and destructive actions.
- **Icon buttons:** Transparent background, `opacity: 0.8` default → `1.0` on hover. No border. Used for toolbar actions and inline controls. Disabled state uses `FG2` fill.

**Caption buttons** (window controls) are 48px wide, transparent, with a `#40000000` hover overlay. The close button gets a red overlay on hover.

### Tabs

Tab items use 14px bold text at 0.56 opacity when inactive, transitioning to full opacity and accent color on selection. Padding is `10px 0` with a minimum height of 24px. No border or underline; state is communicated purely through opacity and color shift.

### List items

Repository sidebar items are 24px tall with transparent backgrounds. Hover state uses `AccentHovered` at 50% opacity. Selected state uses `AccentHovered` at full opacity. Focused + selected state uses the primary accent color at 65% opacity, 80% on hover.

### Switch buttons (radio-style toggles)

Pill-shaped (rounded: `14px`) at 24px height. Transparent background when off, accent color when on. Text changes from `FG2` → `FG1` on hover → white when checked. Icon opacity matches: 0.8 → 1.0 → white.

### Tree expanders

9×9px toggle buttons with a chevron path that rotates 90° on expand. `FG2` fill at rest, accent fill on hover/check.

### Data grid / completion list

Completion items are 24px tall. The completion popup uses 4px corner radius with a 1px `border-subtle` border, 8px × 4px padding.

## Do's and Don'ts

- **Do** use `FG2` (secondary text color) for labels, metadata, and inactive states — never for primary content
- **Do** rely on opacity transitions (0.56 → 1.0) for tab and icon state changes
- **Do** use the system accent color exclusively for interactive highlights (focus, selection, primary actions)
- **Do** use tonal surface differences (4–8 RGB steps) to separate layers — not shadows or borders
- **Do** keep text input borders at 1px with accent color on hover/focus
- **Don't** use rounded corners on text inputs, separators, or inline data displays
- **Don't** introduce decorative drop shadows on non-floating elements
- **Don't** mix monospace (JetBrains Mono) and proportional (Inter) in the same text block — use separate styled elements
- **Don't** exceed two font weights (Regular 400, Bold 700) in the same view
- **Don't** use `FG1` primary text color for secondary metadata — use `FG2` instead
- **Don't** apply accent color to decorative elements; reserve it for interactive affordances only
