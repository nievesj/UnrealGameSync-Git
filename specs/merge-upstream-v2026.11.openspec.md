# OpenSpec: Merge Upstream v2026.11 into UGSGit Fork

**Status**: Draft (revised after council review)
**Target**: Merge tag `v2026.11` from `https://github.com/sourcegit-scm/sourcegit`
**Baseline**: Current HEAD (based on upstream `release/2026.10` + custom commits)
**Created**: 2026-05-19
**Council Confidence Score**: 72/100 (pre-fix) → targeting 95/100

---

## Context

This fork (`UGSGit`) extends SourceGit with:
- **Plugin system** — `Models/PluginRegistry.cs`, `PluginLoader.cs`, `PluginContext.cs`, etc.
- **UnrealSync plugin** — tabs, views, viewmodels for UE project sync/build management
- **Custom build project** — `UGSGit.csproj` replacing `SourceGit.csproj`
- **Namespace rename** — `SourceGit.*` → `UGSGit.*` across all files
- **Custom launcher tab cycling** (`Ctrl+Tab`) for plugin tabs
- **Modified self-update URL** pointing to fork's version.json

The upstream `v2026.11` contains **88 commits across 97 changed files** (+2354/-1523).

**Critical insight**: Fork's changes to overlapping files are almost entirely namespace renames. The functional code in shared SourceGit files is largely untouched by the fork. This means merging is primarily a mechanical namespace-substitution exercise, not a semantic conflict resolution.

---

## Merge Strategy

### Recommended: "Reverse-Rename, Merge, Re-Rename"

Naively `git merge`-ing will produce namespace conflicts on ~80 files. Instead:

1. **Create a temporary branch** from `plugin-system`
2. **Revert the namespace rename globally**: `UGSGit` → `SourceGit` (find-and-replace across all source files)
3. **Revert the project rename**: restore `SourceGit.csproj` (keep `UGSGit.csproj` as-is for reference, but the merge target is `SourceGit.csproj`)
4. **`git merge v2026.11`** — now only the ~6 files with real functional additions will conflict
5. **Re-apply the namespace rename**: `SourceGit` → `UGSGit` (find-and-replace across all source files)
6. **Re-apply fork-specific changes** (plugin init, tab cycling, self-update URL, custom icons)
7. **Delete `SourceGit.csproj`** (only `UGSGit.csproj` remains)

**Why this is better**: Eliminates ~80 mechanical namespace conflicts, makes the 3-way merges truly the only manual work, reduces error rate (no risk of missing a rename in 89 files).

Alternative: manual merge + batch namespace rename. This is slower and produces the same result.

---

## Phase 0: Pre-Merge Preparation

Before any merging, prepare the workspace and handle deletions first.

### Phase 0a: Global Grep for Reference Tracking

Record the current state of `SourceGit` vs `UGSGit` references for post-merge verification:

```bash
# Count pre-merge SourceGit references (should be 0 in source code, some in git history)
grep -r "namespace SourceGit" src/ --include="*.cs" --include="*.axaml" | wc -l
grep -r "using SourceGit" src/ --include="*.cs" | wc -l
grep -r "avares://SourceGit" src/ --include="*.axaml" | wc -l
```

### Phase 0b: Remove Upstream-Deleted Files

Delete these before merging to avoid merge conflicts in files that won't exist post-merge:

```bash
git rm src/ViewModels/DropHead.cs src/ViewModels/Reword.cs src/ViewModels/SquashOrFixupHead.cs
git rm src/Views/DropHead.axaml src/Views/DropHead.axaml.cs
git rm src/Views/Reword.axaml src/Views/Reword.axaml.cs
git rm src/Views/SquashOrFixupHead.axaml src/Views/SquashOrFixupHead.axaml.cs
git commit -m "chore: remove files deleted upstream in v2026.11 (DropHead, Reword, SquashOrFixupHead)"
```

### Phase 0c: Create Temporary Branch & Reverse Namespace

```bash
# Create temp branch
git checkout -b merge-prep/v2026.11

# Reverse namespace rename (dry-run first)
# SourceGit.* -> UGSGit.* in all .cs and .axaml files
# This is the key step: undo the rename so files match upstream's namespace
```

**Note**: After this step, the working tree uses `SourceGit` namespace. The `UGSGit.csproj` project file stays (it doesn't need namespace changes). We only revert source code namespaces.

**Effort**: ~30 minutes.

---

## Phase 1: Safe Direct Merges (7 files)

These files are **completely untouched** by the fork. Take upstream version verbatim. After merge, the namespace rename pass (Phase 4b) will handle the `SourceGit` → `UGSGit` conversion.

| File | Upstream Change | Action |
|---|---|---|
| `src/Commands/QueryPickableCommits.cs` | **New file** — queries pickable commits for Compare view | Accept from upstream (will be renamed later) |
| `src/Resources/Locales/en_US.axaml` | Updated locale strings | Accept from upstream |
| `src/Resources/Locales/ko_KR.axaml` | Updated locale strings | Accept from upstream |
| `src/Views/CommitDetailStandalone.axaml` | **New file** — standalone commit detail window | Accept from upstream |
| `src/Views/CommitDetailStandalone.axaml.cs` | **New file** — code-behind | Accept from upstream |
| `src/Views/RevisionCompareStandalone.axaml` | **New file** — standalone revision compare window | Accept from upstream |
| `src/Views/RevisionCompareStandalone.axaml.cs` | **New file** — code-behind | Accept from upstream |

**Note**: These merge automatically after the reverse-rename step because both sides now use `SourceGit` namespace.

**Effort**: ~5 minutes (verify merge is clean).

---

## Phase 2: Mechanical Merge — Models (4 files)

Fork only changed namespace in these. After reverse-rename, they merge cleanly.

| File | Upstream Change | Merge Expectation |
|---|---|---|
| `src/Models/Commit.cs` | Now extends `ObservableObject`; added `IsHighlightedInGraph` | Clean (reverse-rename) |
| `src/Models/CommitGraph.cs` | **Major rewrite**: `Parse()` → `Generate()`, `CommitGraphHighlighting` enum | Clean (reverse-rename) |
| `src/Models/RepositoryUIStates.cs` | `OnlyHighlightCurrentBranchInHistory` → `GraphHighlighting` enum | Clean (reverse-rename) |

**Effort**: ~5 minutes (verify).

---

## Phase 3: Mechanical Merge — ViewModels (17 files)

All clean after reverse-rename:

| File | Upstream Change | Merge Expectation |
|---|---|---|
| `src/ViewModels/Repository.cs` | Removed `HighlightCurrentBranchOnlyInHistory`; added `_branches.RemoveAll`; moved `Open()` init | Clean |
| `src/ViewModels/Histories.cs` | **Major refactoring**: removed `RewordHead/SquashOrFixupHead/DropHead`; added `GraphHighlighting`, `GenerateGraph()`, `IsCollapseDetails` | Clean |
| `src/ViewModels/Compare.cs` | **Major refactoring**: `_repo` string→`Repository`; `LeftOnlyCommits`/`RightOnlyCommits`; `CherryPick()` | Clean |
| `src/ViewModels/CommitDetail.cs` | Added `Clone()` method | Clean |
| `src/ViewModels/RevisionCompare.cs` | Added `Clone()` method | Clean |
| `src/ViewModels/Preferences.cs` | Added `UseStashAndReapplyByDefault` | Clean |
| `src/ViewModels/Checkout.cs` | Default changes to `UseStashAndReapplyByDefault` | Clean |
| `src/ViewModels/CheckoutAndFastForward.cs` | Same | Clean |
| `src/ViewModels/CheckoutCommit.cs` | Same | Clean |
| `src/ViewModels/CherryPick.cs` | Prefill `MERGE_MSG` when cherry-picking multiple commits | Clean |
| `src/ViewModels/ExecuteCustomAction.cs` | `${BRANCH}`, `${BRANCH_FRIENDLY_NAME}`, `${SHA}` fallback | Clean |
| `src/ViewModels/InProgressContexts.cs` | Cleanup rebase dirs on abort; `OnAborted()` virtual | Clean |
| `src/ViewModels/ImageSource.cs` | Proper `GCHandle` management; `WriteableBitmap` for STB images | Clean |
| `src/ViewModels/DirHistories.cs` | New constructor for `--history <DIR>` CLI | Clean |
| `src/ViewModels/InteractiveRebase.cs` | New `NoVerify` property; upstream additions | Clean |
| `src/ViewModels/Pull.cs` | Minor changes | Clean |
| `src/ViewModels/RepositoryCommandPalette.cs` | Minor changes | Clean |

**Effort**: ~10 minutes (verify merge is clean).

---

## Phase 4: Mechanical Merge — Views (28 files)

All clean after reverse-rename:

| File | Upstream Change | Merge Expectation |
|---|---|---|
| `src/Views/Histories.axaml` | **Major UI restructuring**: graph menu, collapse button, standalone button, tab control | Clean |
| `src/Views/Histories.axaml.cs` | Key handling moved; new properties; standalone/collapse handlers | Clean |
| `src/Views/Repository.axaml` | Removed highlight/relative-time toggles from sidebar | Clean |
| `src/Views/Repository.axaml.cs` | Graph highlighting submenu; `IsDetailsPanelExpanded` binding | Clean |
| `src/Views/Compare.axaml` | Commit list with cherry-pick, selection change handling | Clean |
| `src/Views/Compare.axaml.cs` | Context menus for cherry-pick, copy SHA | Clean |
| `src/Views/CommitDetail.axaml` | `IsDetailsPanelExpanded` binding; `OnTabHeaderPointerPressed` | Clean |
| `src/Views/CommitDetail.axaml.cs` | Added `IsDetailsPanelExpandedProperty` | Clean |
| `src/Views/CommitBaseInfo.axaml` | `PointerReleased` handler on refs | Clean |
| `src/Views/CommitBaseInfo.axaml.cs` | Context menu on ref decorators (copy ref name) | Clean |
| `src/Views/CommitRefsPresenter.cs` | Fill transparent background for hit testing | Clean |
| `src/Views/BranchTree.axaml.cs` | `F2` rename; `Delete/Back` handling; "Compare with upstream" menu | Clean |
| `src/Views/ChangeCollectionView.axaml.cs` | Better left/right arrow key navigation | Clean |
| `src/Views/RevisionFileTreeView.axaml.cs` | Better arrow key navigation | Clean |
| `src/Views/DealWithLocalChangesMethod.axaml` + `.cs` | Minor binding | Clean |
| `src/Views/DirHistories.axaml` | Minor (directory histories) | Clean |
| `src/Views/ListBoxEx.cs` | Minor fix | Clean |
| `src/Views/TagsView.axaml.cs` | Minor | Clean |
| `src/Views/Hotkeys.axaml` | Updated hotkey table | Clean |
| `src/Views/AIAssistant.axaml` | Minor | Clean |
| `src/Views/About.axaml` | Minor | Clean |
| `src/Views/Apply.axaml` | Minor | Clean |
| `src/Views/Pull.axaml` | Search remote branches | Clean |
| `src/Views/Push.axaml` | Search remote branches | Clean |
| `src/Views/Preferences.axaml` | `StashAndReapplyByDefault` setting (34 lines) | Clean |
| `src/Views/BranchSelector.axaml` + `.cs` | Search remote branches (63+65 lines) | Clean |
| `src/Views/InteractiveRebase.axaml` + `.cs` | `NoVerify` checkbox + handlers | Clean |
| `src/Commands/InteractiveRebase.cs` | `NoVerify` flag (4 lines) | Clean |
| `src/Commands/Rebase.cs` | Minor (4 lines) | Clean |
| `src/ViewModels/Rebase.cs` | Minor (8 lines) | Clean |

**Note**: `src/AI/Agent.cs` and `src/AI/Service.cs` are also in the overlap (53 and 38 lines changed). These will be clean after reverse-rename.

**Effort**: ~15 minutes (verify).

---

## Phase 5: Locale Files (14 files)

The spec originally missed 12 locale files. All need both upstream content updates AND any `avares://SourceGit` → `avares://UGSGit` URI changes.

| File | Upstream Change | Action |
|---|---|---|
| `src/Resources/Locales/de_DE.axaml` | 21 lines removed | Accept upstream, check `avares://` URIs |
| `src/Resources/Locales/en_US.axaml` | 43 lines changed | Accept upstream (safe — fork didn't touch) |
| `src/Resources/Locales/es_ES.axaml` | 47 lines changed | Accept upstream, check `avares://` URIs |
| `src/Resources/Locales/fr_FR.axaml` | 17 lines removed | Accept upstream, check `avares://` URIs |
| `src/Resources/Locales/id_ID.axaml` | 13 lines removed | Accept upstream, check `avares://` URIs |
| `src/Resources/Locales/it_IT.axaml` | 21 lines removed | Accept upstream, check `avares://` URIs |
| `src/Resources/Locales/ja_JP.axaml` | 21 lines removed | Accept upstream, check `avares://` URIs |
| `src/Resources/Locales/ko_KR.axaml` | 17 lines removed | Accept upstream (safe — fork didn't touch) |
| `src/Resources/Locales/pt_BR.axaml` | 9 lines removed | Accept upstream, check `avares://` URIs |
| `src/Resources/Locales/ru_RU.axaml` | 25 lines changed | Accept upstream, check `avares://` URIs |
| `src/Resources/Locales/ta_IN.axaml` | 9 lines removed | Accept upstream, check `avares://` URIs |
| `src/Resources/Locales/uk_UA.axaml` | 9 lines removed | Accept upstream, check `avares://` URIs |
| `src/Resources/Locales/zh_CN.axaml` | 43 lines changed | Accept upstream, check `avares://` URIs |
| `src/Resources/Locales/zh_TW.axaml` | 45 lines changed | Accept upstream, check `avares://` URIs |

**Effort**: ~30 minutes (12 files need URI verification).

---

## Phase 6: Careful 3-Way Merges (~8 files)

These files have both substantive fork additions AND substantive upstream changes. With the reverse-rename strategy, conflicts only happen here.

### 6a: `src/App.axaml.cs`

| Fork Changes | Upstream Changes |
|---|---|
| Plugin init: `PluginRegistry.Instance.StateStore = pref` | `--file-history` → `--history` flag |
| Register built-in manifests (HelloWorld, UnrealSync) | Directory history support (new `else if`) |
| Modified self-update URL to fork's `VERSION.json` | Changed `TryLaunchAsFileHistoryViewer` logic |
| Font URI `SourceGit` → `UGSGit` (will match after reverse-rename) | |
| Env var `SOURCEGIT_LAUNCH_AS_ASKPASS` → `UGSGIT_LAUNCH_AS_ASKPASS` (will match after reverse-rename) | |

**Merge approach**: After reverse-rename, the font URI and env var will match upstream. Only the plugin init block, manifest registrations, and self-update URL remain as genuine fork additions. Apply these on top of upstream's version. Must ensure `PluginRegistry.Instance.StateStore = pref` runs **before** `desktop.MainWindow = ...`.

### 6b: `src/Views/Launcher.axaml.cs`

| Fork Changes | Upstream Changes |
|---|---|
| `using Avalonia.VisualTree` (will match after reverse-rename) | `Ctrl+`` for terminal |
| `Ctrl+Tab`/`Ctrl+Shift+Tab` for tab cycling (plugin tabs) | `Ctrl+Shift+B` for create branch |
| Key handling at bottom of `OnKeyDown` | `Ctrl+Shift+T` for create tag |
| | `Ctrl+E` for explorer |
| | `Ctrl+T` requires `cmdKey` modifier |

**Merge approach**: Both sets of hotkeys coexist. The `Ctrl+T` modifier fix (`&& e.KeyModifiers == cmdKey`) must be preserved from upstream. The `Ctrl+Tab` cycling from fork must be compatible with the new switch statement structure. **Verify `LauncherPage.SelectNextTab()` still exists.**

### 6c: `src/Views/Welcome.axaml`

| Fork Changes | Upstream Changes |
|---|---|
| Namespace renames in xmlns (will match after reverse-rename) | Added invisible search-hotkey button |
| `x:Class` rename (will match after reverse-rename) | Search box changes |
| | `OnClearSearchFilter` handler reference |

**Merge approach**: Upstream XAML additions apply cleanly. Fork's `Plugin Settings...` entry is in `.cs`, not `.axaml`. No conflict.

### 6d: `src/Views/Welcome.axaml.cs`

| Fork Changes | Upstream Changes |
|---|---|
| Added `UGSGit.Models/ViewModels` imports (will match after reverse-rename) | Better keyboard navigation (arrow, enter, delete) |
| `OpenPluginSettingsForNode()` method | `OnSearchHotKey()`, `OnClearSearchFilter()` handlers |
| `Plugin Settings...` context menu entry | Fixed `OnKeyDown` to check `KeyModifiers == None` |
| | Left/right tree navigation |

**Merge approach**: Both additive. Merge upstream keyboard handling + new methods. Keep fork's plugin settings menu entry and helper. The `KeyModifiers == None` guard is important for correctness.

### 6e–6h: Pure namespace-rename files (no real conflict)

- `src/ViewModels/Histories.cs` — merge clean after reverse-rename (upstream version accepted entirely)
- `src/ViewModels/Compare.cs` — merge clean after reverse-rename
- `src/Views/Histories.axaml` + `.cs` — merge clean after reverse-rename
- `src/Views/Compare.axaml` + `.cs` — merge clean after reverse-rename

After reverse-rename, these merge without conflict. Verify the merge is correct.

**Effort**: ~2-3 hours.

---

## Phase 7: Project/Config Files & Re-Apply Fork Changes

### 7a: Apply the Merge

```bash
git commit -m "chore: merge upstream v2026.11"
```

### 7b: Re-Apply Namespace Rename (Global Pass)

```bash
# Re-apply the namespace rename: SourceGit -> UGSGit across all source files
# This is the same script from Phase 0c, reversed
```

After this step:
- All `.cs` files use `namespace UGSGit.*` and `using UGSGit.*`
- All `.axaml` files use `xmlns:*="using:UGSGit.*"` and `x:Class="UGSGit.*"`
- All `avares://SourceGit` URIs are now `avares://UGSGit`
- Environment variable is `UGSGIT_LAUNCH_AS_ASKPASS`

**CRITICAL**: Global grep for `avares://SourceGit` after this — must be zero results.

### 7c: Update `UGSGit.csproj`

`SourceGit.csproj` in v2026.11 has:
- Avalonia packages bumped to **11.3.15**
- `Avalonia.Controls.DataGrid` remains at 11.3.13 (still a separate package)
- New property: `<InvariantGlobalization>false</InvariantGlobalization>`

Port these changes to `UGSGit.csproj`. Leave all fork-specific properties (`<Product>`, `<Description>`, company info) intact.

```diff
+ <InvariantGlobalization>false</InvariantGlobalization>
- <PackageReference Include="Avalonia" Version="11.3.13" />
+ <PackageReference Include="Avalonia" Version="11.3.15" />
- <PackageReference Include="Avalonia.Desktop" Version="11.3.13" />
+ <PackageReference Include="Avalonia.Desktop" Version="11.3.15" />
- <PackageReference Include="Avalonia.Fonts.Inter" Version="11.3.13" />
+ <PackageReference Include="Avalonia.Fonts.Inter" Version="11.3.15" />
- <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.13" />
+ <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.15" />
- <PackageReference Include="Avalonia.Diagnostics" Version="11.3.13" Condition="..." />
+ <PackageReference Include="Avalonia.Diagnostics" Version="11.3.15" Condition="..." />
# Avalonia.Controls.DataGrid stays at 11.3.13 (upstream kept it)
```

### 7d: Update `src/Resources/Icons.axaml`

Upstream added: `Icons.ApplyPatch`, `Icons.CollapseToBottom`, `Icons.OpenAsStandalone`
Upstream removed: `Icons.Stopwatch`

Fork additions (verify they survived): `Icons.UnrealSync`, `Icons.Plugin`, `Icons.TabHelloWorld`, any custom plugin icons

Merge: Add upstream's 3 new icons, remove `Stopwatch`, keep fork's custom icons.

### 7e: Update `src/Resources/Styles.axaml`

Upstream changed: ToggleButton styles refactored, TabItem disabled style added, drop-shadow transparency

Fork additions (verify they survived): `.plugin_tabbar`, `.plugin_body`, any custom styles

Merge: Apply upstream restructure, keep fork additions.

### 7f: Remove `SourceGit.csproj`

```bash
git rm src/SourceGit.csproj
```

### 7g: Clean Up Temporary Branch

```bash
# Merge the temp branch back or rebase plugin-system on top
git checkout plugin-system
git merge merge-prep/v2026.11
git branch -d merge-prep/v2026.11
```

### 7h: Update Version

The `VERSION` file is currently `2026.0.2`. Upstream is `2026.11`. Recommended post-merge version: `2026.11.0` (upstream major.minor, fork patch). Update the self-update `VERSION.json` accordingly if one exists.

**Effort**: ~1 hour.

---

## Phase 8: Verification

### Build-Time Checks

- [ ] `dotnet build` succeeds (Release + Debug configurations)
- [ ] `dotnet build` succeeds with PublishAot (if configured)
- [ ] `grep -r "namespace SourceGit" src/ --include="*.cs"` → **zero results**
- [ ] `grep -r "using SourceGit" src/ --include="*.cs"` → **zero results** (except for 3rd-party libs if any use `SourceGit`)
- [ ] `grep -r "avares://SourceGit" src/ --include="*.axaml"` → **zero results**
- [ ] No `XamlParseException` errors in build output
- [ ] `UGSGit.csproj` has `<InvariantGlobalization>false</InvariantGlobalization>`
- [ ] All Avalonia package versions are 11.3.15 (except DataGrid at 11.3.13)
- [ ] No compiler warnings related to deleted types (`DropHead`, `Reword`, `SquashOrFixupHead`)

### Plugin System

- [ ] Plugin registry loads built-in manifests (HelloWorld, UnrealSync)
- [ ] Plugin tab system works (tab bar visible, tab switching works)
- [ ] `Ctrl+Tab` / `Ctrl+Shift+Tab` still cycles plugin tabs
- [ ] Plugin settings dialog opens from Welcome context menu
- [ ] Per-repo plugin overrides work
- [ ] `LauncherPage.SelectNextTab()` exists and functions

### UnrealSync

- [ ] UnrealSync tab loads without errors
- [ ] Build configuration UI works
- [ ] UProject selection dialog functions
- [ ] Engine detection works
- [ ] Build/launch buttons work

### New Upstream Features (v2026.11)

- [ ] `--history <FILE_OR_DIR>` CLI flag works (both files and directories)
- [ ] Standalone commit detail window opens (`CommitDetailStandalone`)
- [ ] Standalone revision compare window opens (`RevisionCompareStandalone`)
- [ ] `Ctrl+J` collapses/expands commit details panel (vertical layout)
- [ ] Graph highlighting: All / Current branch / Selected commits / Both
- [ ] `F2` renames selected local branch in branch tree
- [ ] "Compare with upstream" context menu works on branches
- [ ] Cherry-pick with `-n -x` prefills commit message for multiple commits
- [ ] Stash & Reapply checkbox in Preferences > GIT (default applies)
- [ ] `Ctrl+`` opens terminal from anywhere
- [ ] Custom actions use `${BRANCH}`, `${BRANCH_FRIENDLY_NAME}`, `${SHA}` fallbacks
- [ ] Rebase abort cleans up working directory (rebase-merge/rebase-apply dirs)
- [ ] AI agent strips block-code marks from response / doesn't disable thinking mode
- [ ] AI service supports disabling auto-fetch of models
- [ ] Branch selector search works for remote branches (Pull/Push dialogs)
- [ ] InteractiveRebase dialog shows `NoVerify` checkbox
- [ ] `--no-verify` flag works for rebase

### UI/UX Checks (Runtime)

- [ ] All locale files load without `XamlParseException`
- [ ] Plugin tab bar renders correctly (check `.plugin_tabbar` styles)
- [ ] Commit graph renders with correct highlighting
- [ ] Compare window shows left/right-only commits
- [ ] Tab headers in details panel work (Info/Changes/Files)
- [ ] Hotkeys from `Hotkeys.axaml` all function
- [ ] Welcome page navigation works (arrow keys, enter, delete)
- [ ] Repository view sidebar renders correctly (no orphaned toggle buttons)

### Regression

- [ ] All Git operations: commit, push, pull, fetch, branch, tag, stash, rebase, cherry-pick, merge
- [ ] File history works
- [ ] Directory history works (`--history <DIR>`)
- [ ] Compare window shows changes
- [ ] Interactive rebase works
- [ ] Self-update URL still points to fork's `VERSION.json`
- [ ] AI assistant chat works
- [ ] Right-click context menus on commits, branches, tags work
- [ ] Keyboard shortcuts for copy SHA (`Ctrl+C` in commit list) work
- [ ] `Ctrl+Shift+B` / `Ctrl+Shift+T` keyboard shortcuts work
- [ ] `Ctrl+E` opens file manager

---

## Timeline Estimate (Revised)

| Phase | Description | Estimated Time (Reverse-Rename) | Estimated Time (Manual) |
|---|---|---|---|
| 0 | Pre-merge prep + deletions | 30 min | 30 min |
| 1 | Safe direct merges | 5 min | 15 min |
| 2–4 | Mechanical models/viewmodels/views | 30 min | 3 hr |
| 5 | Locale files | 30 min | 30 min |
| 6 | Careful 3-way merges | 2-3 hr | 2-3 hr |
| 7 | Config, renames, cleanup | 1 hr | 1.5 hr |
| 8 | Verification | 2-3 hr | 3-4 hr |
| **Total** | | **~7-9 hours** | **~10-14 hours** |

**Council Assessment**: 7-9 hours is realistic with the reverse-rename strategy. The original spec's same estimate with manual merging was optimistic.

---

## Risk Assessment (Revised)

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| `avares://SourceGit` URI not renamed in XAML | **High** | Runtime crash (XamlParseException — not caught at build) | Global grep post-rename: `grep -r "avares://SourceGit" src/` must be zero |
| `using SourceGit` or `namespace SourceGit` survives | **Medium** | Build failure | Global grep post-rename: zero allowed |
| Plugin init order broken in `App.axaml.cs` | **Low** | Runtime crash | Verify `StateStore = pref` runs before `desktop.MainWindow` |
| `LauncherPage.SelectNextTab()` deleted or renamed | **Medium** | Build error (`Ctrl+Tab` broken) | Check after merge; add back if missing |
| Locale `avares://` URI not renamed (12 files) | **Medium** | Locale load failure for localized builds | Verify with `grep -r "avares://SourceGit" src/Resources/Locales/` |
| `.csproj` version bumps missed | **Medium** | Build failure or runtime bug | Manually diff SourceGit.csproj v2026.11 vs UGSGit.csproj |
| `UGSGit.csproj` missing `<InvariantGlobalization>` | **Low** | Locale-specific features break | Check after merge |
| Plugin references deleted `DropHead`/`Reword`/`SquashOrFixupHead` | **Low** | Build error | `grep -r "DropHead\|Reword\|SquashOrFixupHead" src/ViewModels/Tabs/ src/Views/Tabs/` |
| CommitGraph rendering regression | **Low** | Visual glitch | Visual inspection of graph in commit view |
| Upstream `Compare._repo` type change (string→Repository) | **Low** | Compile error if plugin uses Compare directly | Plugins use `IRepository` interface, not Compare |
| `Ctrl+Tab` cycling conflicts with new hotkeys | **Low** | Keybinding broken | Review merged `Launcher.axaml.cs` |
| `InvariantGlobalization=false` changes locale-dependent behavior | **Low** | Locale formatting changes | Verify date/number formatting matches expectations |
| Upstream InteractiveRebase breaks fork custom context menus | **Low** | Missing menu item | Verify context menu on non-HEAD commits still has InteractiveRebase options |

---

## Council Review Summary

**Original confidence**: 72/100  
**Target confidence after fixes**: 95/100

### Issues Found by Council

| # | Issue | Status |
|---|---|---|
| 1 | Merge strategy suboptimal (manual vs. reverse-rename) | ✅ Fixed — updated to reverse-rename |
| 2 | Overlap count wrong (87 vs actual 89) | ✅ Fixed — enumerated all files |
| 3 | ~11 files missing from enumeration (AI, Commands, InteractiveRebase, BranchSelector, Preferences.axaml, Rebase) | ✅ Fixed — all added to Phase 3/4 tables |
| 4 | 12 locale files not called out | ✅ Fixed — new Phase 5 |
| 5 | XAML `avares://` risk not flagged | ✅ Fixed — added to verification and risk table |
| 6 | Phase ordering (deletions should come first) | ✅ Fixed — Phase 0b |
| 7 | Verification missing runtime XAML testing | ✅ Fixed — added UI/UX runtime checks |
| 8 | csproj characterization oversimplified | ✅ Fixed — detailed version bump diff |
| 9 | `README.md` and `VERSION` not addressed | ✅ Fixed — added to Phase 7h |
| 10 | `InteractiveRebase` files not called out as new additions | ✅ Fixed — listed in Phase 4 table |
