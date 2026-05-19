# UGSGit — Agent Instructions

## Project

Avalonia-based desktop Git GUI client with Unreal Engine workspace sync.  
Fork of [SourceGit](https://github.com/sourcegit-scm/sourcegit) with the `UnrealSync` plugin.  
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

- Two built-in manifest plugins (always available): `HelloWorldPluginManifest`, `UnrealSyncManifest`
- External plugins: drop `.dll` files into `plugins/` directory beside the executable
- Plugin discovery happens at startup in `App.TryLaunchAsNormal()`
- Built-in manifests registered **before** external discovery (collision detection prevents overrides)
- Plugin state store is `Preferences` itself (injected via `IPluginStateStore`)

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
