# UGSGit — Agent Instructions

## Project

Avalonia-based desktop Git GUI client with Unreal Engine workspace sync.  
Fork of [SourceGit](https://github.com/sourcegit-scm/sourcegit).

### Divergence from upstream SourceGit

| Aspect | SourceGit | UGSGit |
|--------|-----------|--------|
| Namespace | `SourceGit` | `UGSGit` |
| Assembly name | `SourceGit` | `ugsgit` |
| Plugin system | None | Adds full plugin system (manifests, external DLL loading, per-repo enable/disable) |
| Tabs | Hard-coded: Repository + settings/dialogs | Extensible via `IRepositoryTab`; each plugin contributes tabs |
| Startup args | Standard desktop | Adds `--history`, `--blame`, `--core-editor`, `--rebase-todo-editor`, `--rebase-message-editor`, `UGSGIT_LAUNCH_AS_ASKPASS` |
| Built-in plugins | N/A | `HelloWorld` (reference) and `UnrealSync` (Unreal Engine workspace sync) |

Namespace: `UGSGit`.

## Quick Start

```sh
dotnet restore
dotnet build
dotnet run --project src/UGSGit.csproj
```

Requires .NET SDK 10 (see `global.json`: SDK 9.0.0, `rollForward: latestMajor`).

## Key Commands

| Action | Command |
|--------|---------|
| Build | `dotnet build -c Release` |
| Publish (Release + AOT) | `dotnet publish src/UGSGit.csproj -c Release -o publish -r <RID>` |
| Format check | `dotnet format --verify-no-changes src/UGSGit.csproj` |
| Format fix | `dotnet format src/UGSGit.csproj` |
| Run | `dotnet run --project src/UGSGit.csproj` |

Runtime IDs: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`.

## Architecture

- **Single project** (`src/UGSGit.csproj`), single solution (`UGSGit.slnx`)
- **MVVM** with CommunityToolkit.Mvvm, ViewModels in `src/ViewModels/`, Views in `src/Views/`
- **Models** in `src/Models/`, **Commands** (git wrappers) in `src/Commands/`, **Services** in `src/Services/`
- **Native/OS abstraction** in `src/Native/`: `Windows.cs`, `MacOS.cs`, `Linux.cs` implement `IBackend`
- **App entrypoint**: `App.Main()` in `src/App.axaml.cs` — classic desktop lifetime
- **UnrealSync plugin** (built-in): `src/ViewModels/Tabs/UnrealSync/`, `src/Views/Tabs/UnrealSync/`

## Native AOT & Build Modes

- **Release builds** default to Native AOT (`PublishAot=true`, `PublishTrimmed=true`, `TrimMode=link`)
- **Plugin loading is incompatible with AOT**. Set `DisableAOT=true` to test plugins in Release:
  ```sh
  dotnet publish -c Release -p:DisableAOT=true -r win-x64 src/UGSGit.csproj
  ```
- **Update detection** can be disabled: `-p:DisableUpdateDetection=true`
- AOT builds define `DISABLE_PLUGINS` (plugins/ folder is ignored at runtime)

## Plugin System

### Plugin lifecycle

1. **Startup** (`App.TryLaunchAsNormal()`): built-in manifests (`HelloWorldPluginManifest`, `UnrealSyncManifest`) register into `PluginRegistry.Instance` via `RegisterBuiltInManifest()`
2. **Discovery** (`PluginLoader.Discover()`): scans `<executable>/plugins/` for `.dll` files, loads each in an isolated `AssemblyLoadContext`, finds the first `IPluginManifest` implementation. Collision detection prevents a DLL from overriding a built-in
3. **Per-repo activation** (`LauncherPage.RegisterBuiltInTabs()` → `PluginActivator.ActivateEnabledPlugins()`): when a repo tab opens, `PluginActivator` iterates all manifests and calls `manifest.CreateTabs(context)` for each enabled plugin. The resulting `IRepositoryTab` instances are appended to the tab bar after the main Repository tab
4. **Runtime toggle**: plugin state changes (`PluginStateChanged` event on `PluginRegistry`) trigger `LauncherPage.OnPluginStateChanged`, which calls `PluginActivator.ActivatePlugin()` or `DeactivatePlugin()` to add/remove tabs live

### Key types

| Type | Role |
|------|------|
| `IPluginManifest` | Plugin entry point — a DLL must have one class implementing this |
| `IPluginStateStore` | Persistence abstraction — implemented by `Preferences` |
| `PluginRegistry` | Singleton: holds all manifests, manages enabled state (per-repo override → global default → manifest default) |
| `PluginLoader` | Static: discovers external DLLs in `plugins/` dir |
| `PluginActivator` | Static: creates/destroys `IRepositoryTab` instances from manifests |
| `PluginContext` | Provides repo path, name, git dir, and `IsFirstLoadForRepository` flag |
| `IRepositoryTab` | Each plugin tab contributes a `BodyContent` (main view) and `ToolbarContent` (status bar) |

### Plugin resolution order (enabled state)

Per-repo override → global default → `manifest.IsGlobalByDefault`

- Per-repo overrides stored in `RepositoryUIStates.PerRepoPluginOverrides` (a dictionary serialized per repo)
- Global defaults stored in `Preferences` via `IPluginStateStore`
- UI for managing plugins: `PluginSettingsView` (global) and `PerRepoPluginDialog` (per-repo)

## Code Style (enforced via .editorconfig)

- **Private fields**: `_camelCase` | **Private static fields**: `s_` prefix | **Constants**: PascalCase
- **Braces**: Allman (always on new line)
- **`this.` qualification**: avoid unless necessary
- **`var`**: prefer everywhere
- **Expression-bodied**: properties/indexers/accessors yes; methods/constructors/operators no
- **Primary constructors**: preferred
- **File-scoped namespaces**: used in newer files
- **`dotnet_diagnostic.CS0649`** (unassigned field): error
- **Avalonia analyzers** (`AVP*`, `AVADEV*`, `AVLN*`): mostly error severity
- **XAML/AXAML files**: 2-space indent; `.csproj` / `.props` / `.json`: 2-space indent

## Branch & PR Convention

- **PRs target `develop`**, not `main`
- CI runs on `develop` push and PRs
- Release workflow exists in `.github/workflows/release.yml` (not checked in, used by CI)

## Localization

- Locale files: `src/Resources/Locales/*.axaml` (keyed by locale code like `en_US`, `de_DE`)
- Helper: `python translate_helper.py <LOCALE_CODE> [--check]`
- CI auto-formats locale files and updates `TRANSLATION.md` on push to locale paths

## Testing

- **No tests in the main project.** Only the `depends/AvaloniaEdit` submodule has tests.
- To run submodule tests: `dotnet test depends/AvaloniaEdit/test/AvaloniaEdit.Tests/AvaloniaEdit.Tests.csproj`

## Submodule

`depends/AvaloniaEdit` — custom fork of AvaloniaEdit at `https://github.com/love-linger/AvaloniaEdit.git`  
Clone with `--recurse-submodules` or `git submodule update --init --recursive`.

## Versioning

- Single source of truth: `VERSION` file (currently `2026.0.3`)
- Read by `src/UGSGit.csproj` at build time via `File.ReadAllText`

## Data Storage

| OS | Path |
|----|------|
| Windows | `%APPDATA%\SourceGit` |
| Linux | `~/.sourcegit` |
| macOS | `~/Library/Application Support/SourceGit` |

Portable mode: create a `data` folder next to the executable (Windows/Linux AppImage only).

## Important Constraints

- **MSYS Git is NOT supported on Windows.** Use official Git for Windows.
- Git >= 2.25.1 required.
- macOS builds from GitHub Releases are unsigned. Either use Homebrew or run `sudo xattr -cr /Applications/UGSGit.app`.
- Linux: `AVALONIA_SCREEN_SCALE_FACTORS` may need tuning for HiDPI. `AVALONIA_IM_MODULE=none` if accented characters don't work.
