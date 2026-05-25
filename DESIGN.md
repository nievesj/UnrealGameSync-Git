---
version: alpha
name: SourceGit
description: >-
  Desktop Git GUI client with a minimal, dense, developer-focused aesthetic.
  Built on Avalonia UI with a dual light/dark theme system.
  Visual identity prioritizes information density, muted surface tones,
  and crisp monospace diff rendering over decorative elements.

colors:
  # ── Light theme ──────────────────────────────────────────────────────────
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
  border0-light: "#FFCFCFCF"
  border1-light: "#FF898989"
  border2-light: "#FFCFCFCF"

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
  diff-block-border-light: "#DarkCyan"

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
  primary: "#FFDDDDDD"
  on-surface: "#40F1F1F1"

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
  diff-block-border: "#DarkCyan"

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
    fontFamily: JetBrains Mono
    fontSize: 13px
    fontWeight: 400
  inline-code:
    fontFamily: JetBrains Mono
    fontSize: 13px
    fontWeight: 400

rounded:
  none: 0px
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
    cornerRadius: "{rounded.window}"
    backgroundColor: transparent
    dropShadow: "0 0 12 #60000000"
  window-frameless-maximized:
    padding: 0px
    cornerRadius: 0px
  tooltip:
    backgroundColor: "{colors.popup}"
    foregroundColor: "{colors.primary}"
    borderColor: "{colors.border-separator}"
    cornerRadius: "{rounded.md}"
    padding: "{spacing.sm},6px"
    dropShadow: "0 0 8 #30000000"
  flyout:
    backgroundColor: "{colors.popup}"
    cornerRadius: "{rounded.md}"
    padding: "{spacing.lg}"
    dropShadow: "0 0 4 #A0000000"
  context-menu:
    backgroundColor: "{colors.popup}"
    cornerRadius: default
    padding: "{spacing.md},{spacing.sm}"
    dropShadow: "0 0 4 #A0000000"
  menu-item:
    height: 28px
    cornerRadius: 3px
    iconColumnWidth: 28px
  menu-separator:
    margin: "28px,0,4px,0"
  scrollbar-vertical:
    width: 8px
    scrollbar-horizontal:
    height: 8px
  scrollbar-thumb:
    cornerRadius: "{rounded.md}"
  text-input:
    cornerRadius: 0px
    padding: "4px,0"
    minHeight: 16px
    borderColor: "{colors.border-interactive}"
    backgroundColor: "{colors.contents}"
    textColor: "{colors.primary}"
  text-input-hover:
    borderColor: system-accent
  text-input-focus:
    borderColor: system-accent
  caption-button:
    width: 48px
    backgroundColor: transparent
    cornerRadius: 0px
  caption-button-hover:
    backgroundColor: "#40000000"
  caption-button-close-hover:
    backgroundColor: red
  icon-button:
    backgroundColor: transparent
    opacity: 0.8
  icon-button-hover:
    opacity: 1
  icon-button-disabled:
    foregroundColor: "{colors.on-surface}"
  flat-button:
    borderWidth: 1px
    borderColor: "{colors.border-separator}"
    backgroundColor: "{colors.flat-button-bg}"
    textColor: "{colors.primary}"
    fontWeight: bold
    cornerRadius: default
  flat-button-hover:
    backgroundColor: "{colors.flat-button-bg-hovered}"
  flat-button-primary:
    textColor: accent-button-foreground
    borderColor: accent-button-border
    backgroundColor: accent-button-background
  flat-button-primary-hover:
    backgroundColor: accent-button-background-hover
  list-item:
    height: "{spacing.list-item-height}"
    padding: 0
  list-item-hover:
    backgroundColor: system-list-low
    opacity: 0.5
  list-item-selected:
    backgroundColor: system-list-low
    opacity: 1
  checkbox:
    size: 16x16px
    cornerRadius: "{rounded.sm}"
  checkbox-hover:
    borderColor: system-accent
  checkbox-checked:
    accentFill: system-accent
  checkbox-focus:
    borderColor: system-accent
    borderWidth: 2px
  radioButton:
    outerSize: 14x14px
    innerDotSize: 10x10px
  switch-button:
    height: 24px
    cornerRadius: "{rounded.pill}"
  comboBox:
    minHeight: 20px
    borderColor: "{colors.border-interactive}"
    itemCornerRadius: "{rounded.sm}"
  tab-item:
    fontSize: 14px
    padding: "10px,0"
    minHeight: 24px
  completion-list:
    backgroundColor: "{colors.popup}"
    borderColor: "{colors.border-subtle}"
    padding: "{spacing.md},{spacing.sm}"
    cornerRadius: "{rounded.lg}"
    itemHeight: 24px
    itemCornerRadius: "{rounded.lg}"
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

**Text** uses two levels. `FG1` (`#DDDDDD`) is the primary text color across all surfaces. `FG2` is a lower-opacity variant (`#F1F1F1` at 25%) used for secondary labels, metadata, and inactive states.

**Borders** follow a similar three-tier pattern: `#181818` for structural borders, `#404040` for separators within content, and `#7C7C7C` for interactive borders (text inputs, combo boxes).

**Diff colors** use translucent fills at two opacity levels — background washes and line-level highlights — ensuring diff hunks are distinguishable without overwhelming the code content.

### Light theme

The light theme mirrors the dark structure with inverted luminance:

- **Content layer (`#FAFAFA`):** Near-white surface for editing areas.
- **Window/chrome layer (`#F0F5F9`):** A cool blue-gray for toolbars and window backgrounds.
- **Titlebar (`#CFDEEA`):** A stronger blue-gray providing visual framing.

Primary text is `#1F1F1F`, secondary text is `#6F6F6F`. Interactive borders use `#898989`, while light separators use `#CFCFCF`.

**Links** use classic blue (`#0000EE`) in light mode and a softer blue (`#4DAAFC`) in dark mode. **Commit references** are rendered in `DarkOrange` in both themes.

## Typography

The type system uses two font families with clearly separated roles:

- **Inter** handles all UI text. It is a humanist sans-serif optimized for screen legibility at small sizes, with a tall x-height and open apertures that remain readable at the default 13px.
- **JetBrains Mono** handles all monospace content — commit hashes, diffs, inline code, and code editing. Its distinctive characters (e.g., `0` with a dot, `1` with a serif) reduce ambiguity in data-dense views.

Text styles maintain a strict hierarchy through `FontWeight` rather than size variation:

| Role | Weight | Size | Notes |
|---|---|---|---|
| Tab headers | Bold | 14px | Opacity 0.56 when inactive, 1.0 when selected |
| Group headers | Bold | 13px | Secondary color (`FG2`) |
| Info labels | Bold | 13px | Right-aligned, secondary color |
| Body | Regular | 13px | Default for all controls |
| Body small | Regular | 11px | Decreased from default via converter |
| Menu shortcuts | Regular | 11px | Right-aligned, secondary foreground |
| Commit links | Regular+Underline | — | DarkOrange color |

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

- **Primary (accent-filled):** Uses system accent colors (`AccentButtonBackground`, `AccentButtonForeground`). Bold weight. Border 1px. The single most prominent action per view.
- **Flat (outlined):** Border 1px (`Border2`), bold weight, fills to `FlatButton.Background` on hover. Used for secondary and destructive actions.
- **Icon buttons:** Transparent background, 0.8 opacity default → 1.0 on hover. No border. Used for toolbar actions and inline controls. Disabled state uses `FG2` fill.

**Caption buttons** (window controls) are 48px wide, transparent, with a `#40000000` hover overlay. The close button gets a red overlay on hover.

### Tabs

Tab items use 14px bold text at 0.56 opacity when inactive, transitioning to full opacity and accent color on selection. Padding is `10px 0` with a minimum height of 24px. No border or underline; state is communicated purely through opacity and color shift.

### List items

Repository sidebar items are 24px tall with transparent backgrounds. Hover state uses `AccentHovered` at 50% opacity. Selected state uses `AccentHovered` at full opacity. Focused + selected state uses the primary accent color at 65% opacity, 80% on hover.

### Switch buttons (radio-style toggles)

Pill-shaped (corner radius 12px) at 24px height. Transparent background when off, accent color when on. Text changes from `FG2` → `FG1` on hover → white when checked. Icon opacity matches: 0.8 → 1.0 → white.

### Tree expanders

9×9px toggle buttons with a chevron path that rotates 90° on expand. `FG2` fill at rest, accent fill on hover/check.

### Data grid / completion list

Completion items are 24px tall. The completion popup uses 4px corner radius with a 1px `Border0` border, 8px x 4px padding.

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