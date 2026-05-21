# OpenSpec: Editor Binary Sync

## Problem

UGSGit can build and publish editor binaries to a network share, but it cannot
*consume* them. The "Sync Now" button only runs `git pull --rebase` — there is
no step that downloads and deploys precompiled editor binaries. Users have no
visibility into which editor build they have, whether it matches their current
commit, or whether a newer build is available.

The original Perforce-based UGS handles this as a core feature: after syncing
source, it downloads the matching precompiled editor zip, removes old binaries
(via manifest), extracts the new ones into the project directory, and writes a
new manifest for future cleanup.

## Current State

| Capability                        | Status                                 |
|-----------------------------------|----------------------------------------|
| Git pull (source code)            | ✅ `GitSyncService.SyncToLatestAsync()` |
| Build locally                     | ✅ `BuildService.ExecuteAllAsync()`     |
| Package to zip locally            | ✅ `BuildGraphService.CreateZipAsync()` |
| Publish zip to network share      | ✅ `PublishService.PublishZipAsync()`   |
| Download zip from network share   | ❌                                      |
| Extract zip to project directory  | ❌                                      |
| Remove old binaries before deploy | ❌                                      |
| Manifest tracking                 | ❌                                      |
| Editor process/lock detection     | ❌                                      |
| UI showing current build state    | ❌                                      |
| Skip if already synced            | ❌                                      |

## Network Share Layout

```
\\EMBYSERVER\Builds\UnrealEngine\Wardenship\
├── Editor\
│   ├── WardenshipEditor-294d927c8.zip
│   ├── WardenshipEditor-a3f5b1d02.zip
│   └── WardenshipEditor-7e9c4a3f1.zip
└── Game\
    └── ...
```

- **Path**: `{NetworkBase}/{EditorChannel}/`
- **Naming**: `{ProjectName}Editor-{shortSha}.zip`
- **Short SHA**: 9-character git commit abbreviation (matches `git rev-parse --short=9`)

## Terminology Note

The existing codebase uses "Archive" in `UgsConfig` for *packaging* configuration
(`UgsArchiveConfig`, `archive` JSON section). To avoid confusion, the download-side
concept uses "Deploy" terminology: `DeployBuildInfo`, `DeployResult`, etc.
Models shared with the plugin contract go in `PluginAbstractions`; the service
interface and implementation are host-only.

## Reference: Original UGS Flow

1. **Discover** — `p4 filelog` on depot path, parses `[CL123456]` in descriptions
2. **Download** — `p4 print` streams zip to temp file
3. **Remove old** — Reads `.zipmanifest`, deletes matching files (size+timestamp safety)
4. **Extract** — `ZipArchive` → `entry.ExtractToFile(..., overwrite: true)` into project root
5. **Write manifest** — Lists all extracted files with sizes and timestamps (temp file → atomic rename)
6. **Cleanup** — Deletes temp zip in `finally` block
7. **Lock detection** — `File.OpenWrite()` on editor binaries, catches `IOException`
8. **Skip** — Tracks `LastSyncEditorArchive` key, skips if same

## Proposed Flow

### 1. Discover Available Builds

**Input**: `NetworkBase`, `EditorChannel`, current commit SHA
**Output**: List of available builds with their commit SHAs

```
Scan: {NetworkBase}/{EditorChannel}/*.zip
Parse: filename → short SHA (regex: (?i){projectName}Editor-([0-9a-f]+)\.zip)
Match: exact commit match only (see resolution below)
```

**Interface**:

```csharp
public record DeployBuildInfo(string ShortSha, string ZipPath, long FileSize, DateTime LastModified);

public interface IDeployService
{
    /// <summary>Lists available editor builds on the network share.</summary>
    /// <remarks>
    /// Partial failures (permission errors on individual zips) are logged as warnings
    /// and the unreadable builds are skipped. Only fatal failures (network share
    /// completely unreachable) cause an exception.
    /// </remarks>
    Task<IReadOnlyList<DeployBuildInfo>> DiscoverAsync(string networkBase, string channel, string projectName, CancellationToken ct);

    /// <summary>Finds the build matching the exact commit SHA.</summary>
    Task<DeployBuildInfo?> FindBuildForCommitAsync(string networkBase, string channel, string projectName, string commitSha, CancellationToken ct);
}
```

**Partial error handling**: If individual zip files can't be accessed (permissions,
corruption), `DiscoverAsync` logs a warning and skips them, returning the readable
subset. Only raises an exception when the network share itself is unreachable.

**Exact match only (v1)**: For v1, the service requires an exact commit SHA match.
Ancestor matching (finding the closest ancestor commit that has a build) is deferred
to a future release. Users who need older builds can sync to the exact commit that
has a build available.

**Multiple builds per commit**: If the network share has multiple zips for the same
commit (e.g., re-built after a fix), `FindBuildForCommitAsync` picks the most recent
by `LastModified` time.

### 2. Download

**Input**: `DeployBuildInfo`, local temp directory
**Output**: Path to downloaded zip file

- Copy from UNC path to `<repo>/.unrealsync/deploy.zip` using a chunked
  `FileStream` copy (not `File.Copy`) with progress reporting via `IProgress<long>`
- Editor zips can be multiple GB — a blocking `File.Copy` without progress
  feedback would appear frozen to the user
- Verify file is not zero-length after copy
- If the network share is unreachable, report a clear error (not a crash)

### 3. Editor Lock Detection

**Before extraction**, check if editor binaries are locked by a running process:

```csharp
private static bool IsEditorRunning(string repoPath)
{
    string[] editorBinaries = {
        "Engine/Binaries/Win64/UnrealEditor-Win64-Development.exe",
        "Engine/Binaries/Win64/UnrealEditor-Win64-Debug.exe",
        "Engine/Binaries/Win64/UnrealEditor-Win64-DebugGame.exe",
    };

    foreach (var relative in editorBinaries)
    {
        var path = Path.Combine(repoPath, relative);
        if (File.Exists(path))
        {
            try { using var stream = File.OpenWrite(path); }
            catch (IOException) { return true; }  // locked = editor running
        }
    }
    return false;
}
```

If the editor is running, **abort with a user-facing message** — do not attempt
to overwrite locked files. The user must close the editor first.

**v1 limitation: Windows-only.** The lock detection and binary paths are Windows-specific
(`Win64/UnrealEditor*.exe`). Linux and macOS editor paths (`Engine/Binaries/Linux/`,
`Engine/Binaries/Mac/`) are not handled in v1. The existing `Native/` OS abstraction
layer provides a natural extension point for this in a future release.

### 4. Remove Old Binaries

**Input**: Old manifest file path, project root
**Behavior**: Read manifest, delete files that match (same size + timestamp),
skip files that have been modified by the user.

**Manifest location**: `<repo>/.unrealsync/Editor.zipmanifest`

This matches the existing convention for `UgsWorkspaceState` which lives at
`<repo>/.unrealsync/state.json`.

**Manifest format** (JSON):

```json
{
  "version": 1,
  "commitSha": "294d927c8",
  "files": [
    { "relativePath": "Engine/Binaries/Win64/UnrealEditor.exe", "length": 123456, "lastWriteTimeUtc": "2026-05-21T10:00:00Z" },
    { "relativePath": "Engine/Binaries/Win64/UnrealEditor.pdb", "length": 789012, "lastWriteTimeUtc": "2026-05-21T10:00:00Z" }
  ]
}
```

**Safety checks before deletion** (same as original UGS):

- File must exist
- File size must match manifest
- File timestamp must match manifest (±2 seconds tolerance)
- If any check fails, skip that file (log a warning) — don't delete user-modified files

### 5. Extract

**Input**: Downloaded zip path, project root
**Behavior**: Extract all entries to a temp staging directory first, then atomically
move files into the project root. This prevents the project from being in a broken
half-deployed state if extraction fails partway through (disk full, crash, etc.).

```csharp
var stagingDir = Path.Combine(Path.GetTempPath(), $"ugsgit-deploy-{Guid.NewGuid():N}");
try
{
    var timestamp = DateTime.UtcNow;
    using var zip = ZipFile.OpenRead(zipPath);
    foreach (var entry in zip.Entries)
    {
        if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\")) continue;

        var stagingPath = Path.Combine(stagingDir, entry.FullName);
        Directory.CreateDirectory(Path.GetDirectoryName(stagingPath)!);
        entry.ExtractToFile(stagingPath, overwrite: true);
        File.SetLastWriteTimeUtc(stagingPath, timestamp);
    }

    // Atomic move: copy files from staging to project root
    foreach (var file in Directory.GetFiles(stagingDir, "*", SearchOption.AllDirectories))
    {
        var relativePath = file.Substring(stagingDir.Length + 1);
        var targetPath = Path.Combine(projectRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.Move(file, targetPath, overwrite: true);
    }
}
finally
{
    if (Directory.Exists(stagingDir))
        Directory.Delete(stagingDir, recursive: true);
}
```

Set all extracted file timestamps to `DateTime.UtcNow` (same as original UGS —
this makes the manifest timestamp check work for future cleanup).

### 6. Write New Manifest

After extraction, write a new manifest listing all extracted files with their
sizes and the shared timestamp. Write to a temp file first, then atomically
rename (same pattern as original UGS):

```
Write → Editor.zipmanifest.tmp
Rename → Editor.zipmanifest
```

### 7. Update Workspace State

Store the deployed commit SHA in `UgsWorkspaceState`:

```csharp
[JsonPropertyName("lastDeployedArchiveSha")]
public string LastDeployedArchiveSha { get; set; } = string.Empty;
```

This uses `{ get; set; }` (not `init`) — consistent with the existing
`EnginePathOverride` property. Both are runtime-mutable: `EnginePathOverride`
changes when the user selects a different engine, and `LastDeployedArchiveSha`
updates after each binary deploy. The init-only convention applies to config
values that never change after initial load.

This enables:

- **Skip if already synced**: If `LastDeployedArchiveSha` matches the current
  commit, skip the entire download/extract flow
- **UI display**: Show which commit's binaries are currently deployed

### 8. Cleanup

- Delete the temp zip file (`<repo>/.unrealsync/deploy.zip`) in a `finally` block
- Delete the old manifest only after the new one is written (not before — if
  extraction fails midway, the old manifest is still valid for cleanup on the
  next attempt)

## UI Changes

### FullWorkspaceView — Build Status

Add a line to the Project Info section showing the current editor build state:

```
Editor build: 294d927c8 (current)     ← binaries match current commit
Editor build: a3f5b1d02 (behind)       ← binaries are from an older commit
Editor build: none                     ← no precompiled binaries deployed
Editor build: unavailable              ← network share unreachable
```

**ViewModel properties**:

```csharp
[ObservableProperty] private string _editorBuildStatusText = "";
[ObservableProperty] private bool _editorBuildIsCurrent;
[ObservableProperty] private bool _editorBuildAvailable;  // a newer build exists on the share
```

**Computed on init and after sync**:

1. Read `LastDeployedArchiveSha` from workspace state
2. Compare with current commit SHA
3. If network share is reachable, check if a build exists for the current commit

### Sync Button — Integrated Binary Deploy

The "Sync Now" button should perform both steps:

1. `git pull --rebase origin <branch>` (existing)
2. If `NetworkBase` is configured and `EditorChannel` is set:
    - Check if a build exists for the new commit
    - If yes and different from `LastDeployedArchiveSha`: download, extract, deploy
    - If editor is running: warn and skip binary deploy (source sync still succeeds)

The binary deploy step should be **non-blocking** — if it fails, the git sync
still succeeds. The user can manually trigger binary deploy later.

### New Button — Deploy Editor Build

Add a separate "Deploy Editor" button (or make it part of the sync flow with a
checkbox). This allows users to manually deploy binaries without doing a full
sync, useful when they've already synced source but need to update binaries.

## Service Architecture

### New Interface: `IDeployService`

Location: `src/Services/IDeployService.cs` (host-only, not in PluginAbstractions)

This service is a host concern — it requires SMB network access, local file
system manifest management, and git merge-base execution. External plugins
cannot meaningfully implement or consume it. Only the models go in PluginAbstractions
so that built-in plugins like UnrealSync can reference them.

```csharp
public interface IDeployService
{
    Task<IReadOnlyList<DeployBuildInfo>> DiscoverAsync(string networkBase, string channel, string projectName, CancellationToken ct);
    Task<DeployBuildInfo?> FindBuildForCommitAsync(string networkBase, string channel, string projectName, string commitSha, CancellationToken ct);
    Task<DeployResult> DeployAsync(string repoPath, string networkBase, string channel, string projectName, string commitSha, IProgress<string> log, CancellationToken ct);
}
```

### New Model: `DeployManifest` (in PluginAbstractions)

Location: `libs/UGSGit.PluginAbstractions/DeployManifest.cs`

```csharp
public record DeployManifest(int Version, string CommitSha, List<DeployManifestFile> Files);
public record DeployManifestFile(string RelativePath, long Length, DateTime LastWriteTimeUtc);
```

### New Model: `DeployBuildInfo` (in PluginAbstractions)

Location: `libs/UGSGit.PluginAbstractions/DeployBuildInfo.cs`

```csharp
public record DeployBuildInfo(string ShortSha, string ZipPath, long FileSize, DateTime LastModified);
```

### New Model: `DeployResult` (in PluginAbstractions)

Location: `libs/UGSGit.PluginAbstractions/DeployResult.cs`

```csharp
public record DeployResult(DeployStatus Status, string Message);
public enum DeployStatus { Success, NoBuildFound, EditorRunning, NetworkUnavailable, Failed }
```

### Implementation: `DeployService`

Location: `src/Services/DeployService.cs`

Implements `IDeployService`. Handles discovery, chunked download, lock detection,
manifest-based cleanup, staged extraction (temp dir → atomic move), and manifest writing.

### Modified: `UgsWorkspaceState`

Add `LastDeployedArchiveSha` property (mutable, `{ get; set; }` — see rationale above).

### Modified: `FullWorkspaceViewModel`

- Add editor build status properties
- Integrate binary deploy into `SyncAsync`
- Add `DeployEditorAsync` command
- Resolve `IDeployService` via `context.GetService<IDeployService>()`

### Modified: `FullWorkspaceView.axaml`

- Add editor build status line in Project Info section
- Add "Deploy Editor" button (or integrate into sync flow)

### Modified: `PluginActivator`

Register `IDeployService` as a host-scoped service in `PluginContext` so built-in
plugins like UnrealSync can access it via `GetService<IDeployService>()`.

### Modified: `PluginAbstractionsJsonContext`

Register `DeployManifest`, `DeployBuildInfo`, and `DeployResult` for JSON serialization.

## File Summary

| Action | File                                                              | Purpose                                        |
|--------|-------------------------------------------------------------------|------------------------------------------------|
| Create | `src/Services/IDeployService.cs`                                  | Service interface (host-only)                  |
| Create | `libs/UGSGit.PluginAbstractions/DeployManifest.cs`                | Manifest model                                 |
| Create | `libs/UGSGit.PluginAbstractions/DeployBuildInfo.cs`               | Build info model                               |
| Create | `libs/UGSGit.PluginAbstractions/DeployResult.cs`                  | Deploy result model                            |
| Create | `src/Services/DeployService.cs`                                   | Full implementation                            |
| Modify | `libs/UGSGit.PluginAbstractions/UgsWorkspaceState.cs`             | Add `LastDeployedArchiveSha`                   |
| Modify | `libs/UGSGit.PluginAbstractions/PluginAbstractionsJsonContext.cs` | Register `DeployManifest`, `DeployBuildInfo`, `DeployResult` |
| Modify | `libs/plugins/UnrealSync/ViewModels/FullWorkspaceViewModel.cs`    | Status props, deploy command, sync integration |
| Modify | `libs/plugins/UnrealSync/Views/FullWorkspaceView.axaml`           | Build status UI, deploy button                 |
| Modify | `src/ViewModels/PluginActivator.cs`                               | Register `IDeployService` in `PluginContext`   |

## Resolved Design Decisions

| # | Question | Resolution |
|---|----------|------------|
| 1 | Ancestor matching vs exact commit match? | **Exact match for v1.** Simpler, no git command overhead per build. Users sync to the commit with a build. Ancestor matching deferred — revisit if demand surfaces. |
| 2 | Multiple builds per commit? | **Most recent by `LastModified`.** Simple, predictable. |
| 3 | Partial extraction recovery (crash midway)? | **Extract to temp staging dir, then atomic move.** Prevents broken half-deployed state if extraction fails. Unlike original UGS which extracts in-place. |
| 4 | Linux/macOS editor lock detection? | **Windows-only for v1.** Paths are Win64-specific. The Native/ abstraction layer is the extension point for a future release. |

---

## Plugin Extension: Commit Graph Annotations

### Problem

The plugin system is currently a **tab factory** — plugins can render their own
tabs and call host services, but they cannot push data back into the main app's
UI. Specifically, there is no way for a plugin to annotate the git commit graph
with information like "a precompiled editor build is available for this commit."

This is needed so users can see at a glance which commits have builds available
on the network share, without switching to the UnrealSync tab.

### Current Plugin Communication Surface

| Direction     | Mechanism                                  | What It Does                                 |
|---------------|--------------------------------------------|----------------------------------------------|
| Host → Plugin | `PluginContext.GetService<T>()`            | Plugin calls 9 registered service interfaces |
| Host → Plugin | `IRepositoryTab.OnActivated/OnDeactivated` | Lifecycle notifications                      |
| Plugin → Host | `IPluginLogger`                            | One-way logging (fire-and-forget)            |
| Plugin → Host | ❌ Nothing                                  | No way to push data into host UI             |

**Missing**: No event bus, no pub/sub, no `ICommitAnnotator`, no way for plugins
to contribute markers/annotations to the commit graph.

### Proposed: `ICommitAnnotator` Interface

A new interface in `UGSGit.PluginAbstractions` that plugins implement to
contribute annotations to the commit graph. The host queries all registered
annotators when rendering the graph.

```csharp
/// <summary>
/// Implemented by plugins that want to annotate commits in the git graph.
/// The host calls AnnotateAsync() when generating the commit graph.
/// </summary>
public interface ICommitAnnotator
{
    /// <summary>
    /// Returns annotations for the given commits.
    /// Only called for commits currently visible in the graph (not the full history).
    /// </summary>
    /// <param name="commits">Commits to annotate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Map of short SHA → list of annotations. Commits with no annotation can be omitted.
    /// A commit can have multiple annotations (e.g., both code and content badges).</returns>
    Task<IReadOnlyDictionary<string, IReadOnlyList<CommitAnnotation>>> AnnotateAsync(
        IReadOnlyList<CommitRef> commits, CancellationToken ct);
}
```

> **Design note**: The return type is `IReadOnlyDictionary<string, IReadOnlyList<CommitAnnotation>>`
> (SHA → list of annotations) rather than `SHA → single annotation`. A commit can have
> multiple annotations (e.g., both "Code" and "Content" badges from the commit-type-badges
> annotator). The composite-key workaround (`"{sha}:code"`) is avoided — the dictionary key
> is always the raw short SHA.

```csharp
/// <summary>
/// Reference to a commit (SHA only — annotators that need file data should
/// query it via host services themselves).
/// </summary>
/// <param name="ShortSha">9-character git commit abbreviation.</param>
public record CommitRef(string ShortSha);

/// <summary>
/// A single annotation for a commit, displayed as a marker on the commit graph.
/// </summary>
public record CommitAnnotation(
    string Label,           // e.g. "Editor" or "Code"
    string? Tooltip,       // e.g. "Editor build available on network share"
    string AnnotationType  // categorization key for host-side styling/theming
);
```

### How It Works

1. **Registration**: `IPluginManifest` gains an optional `IReadOnlyList<ICommitAnnotator>? CommitAnnotators` property.
   Plugins that want to annotate the graph return their annotator instances. A plugin can
   contribute multiple annotators (e.g., UnrealSync contributes both a build-availability
   annotator and a commit-type classifier).

   Unlike `CreateTabs()` which is called per-repo activation, annotators are collected at
   **plugin registration time** (in `PluginRegistry`) and registered into a **host-level
   singleton** `ICommitAnnotationProvider`. This ensures annotators are available to all
   open repositories, not just the one that activated the plugin.

2. **Host collection**: A new static `HostServices` class in `src/Services/HostServices.cs`
   holds the singleton `ICommitAnnotationProvider`. When `PluginRegistry` registers a
   manifest, it collects any annotators from `manifest.CommitAnnotators` and registers them
   with the provider. This is a host-level concern, not per-repo or per-PluginContext.

3. **Graph generation**: When `Histories.GenerateGraph()` runs, it calls
   `HostServices.AnnotationProvider.GetAnnotationsAsync(commitShas)` with the list of
   commit SHAs being rendered. The provider fans out to all registered annotators
   in parallel and merges results.

4. **Rendering**: Annotations render as **always-visible colored badges on the commit
   line**, identical in placement to the existing branch/tag labels rendered by
   `CommitRefsPresenter`. The annotation badge appears alongside branch/tag labels
   in `Histories.axaml` — rendered by a new `CommitAnnotationPresenter` view placed
   in the same row as the existing `CommitRefsPresenter` (not in `CommitGraph.cs`,
   which only draws bezier curves and dots).

   The tooltip is secondary: hover over the badge to see details like "Editor build
   available on network share." The `AnnotationType` string maps to a themed style
   (color, icon) defined by the host.

### Host-Level Service Registry

A new static class provides singleton cross-cutting services that are not tied to
any single repository or plugin activation:

```csharp
// src/Services/HostServices.cs
namespace UGSGit.Services;

/// <summary>
/// Singleton registry for cross-cutting host services that must be accessible
/// to all repositories and plugins, not scoped to a single PluginContext.
/// </summary>
public static class HostServices
{
    /// <summary>
    /// Collects annotations from all registered plugin annotators.
    /// Populated by PluginRegistry when manifests are registered.
    /// </summary>
    public static ICommitAnnotationProvider AnnotationProvider { get; } = new CommitAnnotationProvider();
}
```

`PluginRegistry` calls `HostServices.AnnotationProvider.Register(annotator)` when
a manifest is registered, and `Unregister(annotator)` when a manifest is removed.
Unlike `PluginContext` which is created per activation, this is a true host singleton.

### UnrealSync's Annotator

The UnrealSync plugin implements `ICommitAnnotator` to show editor build
availability on the commit graph:

```csharp
public class UnrealSyncBuildAnnotator : ICommitAnnotator
{
    private readonly IDeployService _deployService;
    private readonly IConfigService _configService;

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<CommitAnnotation>>> AnnotateAsync(
        IReadOnlyList<CommitRef> commits, CancellationToken ct)
    {
        var result = new Dictionary<string, IReadOnlyList<CommitAnnotation>>();

        var config = _configService.LoadConfig(_repoPath);
        if (string.IsNullOrEmpty(config.NetworkBase))
            return result;

        var builds = await _deployService.DiscoverAsync(
            config.NetworkBase, config.EditorChannel, _projectName, ct);

        var buildShas = builds.Select(b => b.ShortSha).ToHashSet();
        foreach (var sha in commits.Select(c => c.ShortSha).Distinct())
        {
            if (buildShas.Contains(sha))
            {
                result[sha] = new[]
                {
                    new CommitAnnotation(
                        Label: "Editor",
                        Tooltip: "Editor build available on network share",
                        AnnotationType: "build-available")
                };
            }
        }

        return result;
    }
}
```

### Performance Considerations

- **Caching**: `DiscoverAsync` scans the network share directory, which is slow
  over SMB. The annotator should cache the build list with a TTL (e.g., 60 seconds)
  and invalidate on sync. The host should not call `AnnotateAsync` on every
  scroll — only when the commit list refreshes.

- **Batching**: The host passes all visible commit SHAs at once, not one at a
  time. This lets the annotator do a single SMB scan and match all commits against it
  in-memory.

- **Cancellation**: The host passes a `CancellationToken` tied to the graph
  generation. If the user scrolls or switches repos, stale annotation requests
  are cancelled.

- **Failure tolerance**: If an annotator throws or times out, the host logs a
  warning and renders the graph without that annotator's markers. A broken
  plugin must never break the commit graph.

### Host-Side Changes

| Component                   | Change                                                                          |
|-----------------------------|---------------------------------------------------------------------------------|
| `IPluginManifest`           | Add `IReadOnlyList<ICommitAnnotator>? CommitAnnotators { get; }` (default null) |
| `PluginRegistry`            | Collect annotators from manifests on registration, register with `HostServices` |
| `HostServices` (new)        | Static singleton: holds `ICommitAnnotationProvider`; cross-repo service registry |
| `ICommitAnnotationProvider` | New host service (src/Services/): merges annotations from all registered annotators |
| `CommitAnnotationProvider`  | Implementation: fans out to all annotators in parallel, merges results           |
| `Histories` VM              | Call `HostServices.AnnotationProvider.GetAnnotationsAsync()` during `GenerateGraph()`, passing visible commit SHAs |
| `Models.Commit`             | Add `IReadOnlyList<CommitAnnotation>? Annotations` property                      |
| `CommitAnnotationPresenter` (new) | View: renders annotation badges alongside `CommitRefsPresenter` in commit row |
| `Histories.axaml`           | Add `CommitAnnotationPresenter` to commit row DataTemplate                        |

> **Rendering note**: Annotations are NOT rendered in `CommitGraph.cs` (a canvas control
> that draws bezier curves and dots). They are rendered as Avalonia labels in
> `Histories.axaml`, alongside the existing `CommitRefsPresenter`, using a new
> `CommitAnnotationPresenter` view. This matches the existing pattern: branch/tag
> labels are `Decorator` objects rendered via `CommitRefsPresenter`, not in the
> graph canvas.

### AnnotationType Theming

The host defines a mapping from `AnnotationType` string to visual style.

| AnnotationType    | Source     | Color | Label    | Example                       |
|-------------------|------------|-------|----------|-------------------------------|
| `build-available` | UnrealSync | Green | Editor   | Editor build on network share |
| *(future)*        | Host       |       |          | e.g. `ci-passed`, `ci-failed` |

Plugins cannot define arbitrary styles — they use the host's predefined types.
This keeps the graph visually consistent. If a plugin returns an unknown
`AnnotationType`, the host renders it with a default neutral style.

**Plugin-disabled behavior**: When the UnrealSync plugin is disabled, its
`build-available` annotation disappears — the plugin's `ICommitAnnotator` is
simply not registered.

### File Summary (Additional)

| Action | File                                                          | Purpose                                                                 |
|--------|---------------------------------------------------------------|-------------------------------------------------------------------------|
| Create | `libs/UGSGit.PluginAbstractions/ICommitAnnotator.cs`          | Annotator interface                                                     |
| Create | `libs/UGSGit.PluginAbstractions/CommitRef.cs`                 | Commit reference (SHA only, no file lists)                              |
| Create | `libs/UGSGit.PluginAbstractions/CommitAnnotation.cs`          | Annotation model                                                        |
| Create | `src/Services/ICommitAnnotationProvider.cs`                   | Host-side provider interface                                            |
| Create | `src/Services/CommitAnnotationProvider.cs`                    | Host-side provider implementation                                       |
| Create | `src/Services/HostServices.cs`                                | Static host-level service registry                                      |
| Create | `src/Views/CommitAnnotationPresenter.cs`                      | View: renders annotation badges in commit row                           |
| Modify | `libs/UGSGit.PluginAbstractions/IPluginManifest.cs`           | Add `IReadOnlyList<ICommitAnnotator>? CommitAnnotators { get; }`         |
| Modify | `src/Models/PluginRegistry.cs`                                | Collect annotators on manifest registration, register with `HostServices` |
| Modify | `src/ViewModels/Histories.cs`                                 | Call provider during graph generation, pass commit SHAs                 |
| Modify | `src/Models/Commit.cs`                                        | Add `IReadOnlyList<CommitAnnotation>? Annotations` property              |
| Modify | `src/Views/Histories.axaml`                                   | Add `CommitAnnotationPresenter` to commit row DataTemplate               |
| Modify | `libs/plugins/UnrealSync/UnrealSyncManifest.cs`               | Return `UnrealSyncBuildAnnotator` from `CommitAnnotators`                |
