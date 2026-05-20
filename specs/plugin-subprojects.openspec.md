# OpenSpec: Plugin Sub-Projects

**Status**: Revised (after council review)
**Target**: Extract plugins into standalone C# sub-projects while preserving AOT compatibility, runtime plugin loading, and all existing behavior.
**Created**: 2026-05-19
**Council Review Confidence**: 82/100
**Council Recommendation**: Ship Phase 0 + Phase 1 as a single PR (3-8h). Defer Phase 2 (UnrealSync) to a separate spec — effort 2-3× original estimate.

---

## Context

UGSGit currently has a **single-project** architecture (`src/UGSGit.csproj`). Built-in plugins (HelloWorld, UnrealSync) live as namespaces inside this monolithic project. External plugins are loaded at runtime via `AssemblyLoadContext` from a `plugins/` directory.

### Problem

| Pain point | Why it matters |
|---|---|
| No compile-time contract for plugin authors | External plugin devs must reverse-engineer interfaces from the monolith |
| UnrealSync depends on internal Services, Models, Native | Can't extract into a separate project without pulling in most of the main app |
| No NuGet-style distribution | Plugin authors can't `dotnet add reference UGSGit.PluginAbstractions` |
| Single csproj means single build config | Can't build plugins independently or publish them separately |
| HelloWorld is trivial enough to extract but no structure exists | No repeatable pattern for future plugins |

### Goal

```
UGSGit.slnx
├── src/UGSGit.PluginAbstractions/        ← contract library (zero dependencies)
├── src/plugins/HelloWorld/               ← standalone project
├── src/plugins/UnrealSync/               ← standalone project (once decoupled)
└── src/UGSGit.csproj                     ← main app (references all of the above)
```

**Constraints (non-negotiable):**

1. **AOT must still work.** Built-in plugins linked via `ProjectReference` compile into the native binary. `DISABLE_PLUGINS` still disables external DLL loading.
2. **`PluginLoader.Discover()` must still work** for external 3rd-party DLLs.
3. **No circular dependencies.** Main → Abstractions ← Plugin (strict one-way).
4. **Plugin registry, activation, lifecycle unchanged.** Only the build-time layout changes.

### Scope & Recommendation

After council review, this spec covers **two distinct work items**:

| Scope | Recommendation | Effort | Benefit |
|-------|---------------|--------|---------|
| **Phase 0 + Phase 1** (PluginAbstractions + HelloWorld) | **Ship as single PR** | 3–8 hours | ~80% of total benefit: NuGet-packable plugin contract, validated extraction pattern |
| **Phase 2** (UnrealSync decoupling + extraction) | **Defer to separate spec** | 12–20 hours (2–3× original estimate) | ~20% additional benefit: architectural purity for one plugin |

The UnrealSync decoupling was found to have 30+ coupling points across 6 layers (not the 7–12 originally estimated), making it 2–3× more effort than originally scoped. This spec documents the extraction plan but recommends scoping Phase 0 + Phase 1 as the immediate deliverable.

### Alternative D: Abstractions-Only

This spec strongly advocates shipping **Phase 0 + Phase 1** and deferring Phase 2, but the alternative of **only extracting PluginAbstractions** (leaving all built-in plugins in-tree) deserves explicit consideration:

| Criterion | Abstractions-only | Phase 0 + Phase 1 | Full extraction |
|-----------|-------------------|-------------------|-----------------|
| Effort | 1–2 hours | 3–8 hours | 15–28 hours |
| NuGet-packable contract | ✅ Yes | ✅ Yes | ✅ Yes |
| Validated pattern for external plugins | ❌ No (untested) | ✅ HelloWorld proves it | ✅ Proven |
| UnrealSync decoupling | ❌ Not addressed | ❌ Not addressed | ✅ Achieved |
| External plugin DX | Good (NuGet only) | Best (NuGet + example project) | Best |

**Recommendation**: Abstractions-only is acceptable as a minimum viable step, but Phase 1 adds disproportionate value for the effort — a tested HelloWorld project is the template external plugin authors will copy-paste. Ship both.

### Council Review Summary

The spec was reviewed by 3 councillors (deepseek-v4-flash, glm-5.1, gemma4:31b):

| Dimension | Score | Key finding |
|-----------|-------|-------------|
| Completeness | 6–7/10 | Missing: `UnrealSyncJsonContext` placement, ~13+ model types not 5, XAML `DynamicResource` design-time breakage |
| Feasibility | 7–8/10 | Phase 0/1 straightforward; Phase 2a is 2–3× the estimated effort |
| Risk assessment | 5–6/10 | Identifies 6 risks, misses ~7 more (AOT+namespace identity, JsonContext visibility, CI workflow changes) |
| Cost/benefit | 5–7/10 | Phase 0 alone = ~60% benefit at ~15% cost. Phase 2 = ~20% benefit at ~75% cost |
| UnrealSync coupling | 4/10 | 30+ coupling points across 6 layers. ConfigService↔JsonContext circularity is critical path |

**Key recommendations incorporated into this revision:**
1. Defer Phase 2 to separate spec with revised 12–20h estimate
2. Add `UnrealSyncJsonContext` placement as critical design decision
3. Add `TrimmerRootAssembly` entries for new assemblies
4. Add cross-assembly XAML resolution concerns
5. Add design-time XAML preview breakage for plugin projects
6. Add Alternative D discussion

---

## Phase 0: Extract `UGSGit.PluginAbstractions`

The foundation project. Zero dependencies (not even Avalonia or CommunityToolkit.Mvvm).

### New project

`src/UGSGit.PluginAbstractions/UGSGit.PluginAbstractions.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>UGSGit.PluginAbstractions</AssemblyName>
    <RootNamespace>UGSGit.Models</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

### Files to move (from `src/Models/`)

| File | Notes |
|---|---|
| `IPluginManifest.cs` | Unchanged |
| `IRepositoryTab.cs` | Unchanged — `IDisposable` is in `System`, no dependency issue |
| `PluginContext.cs` | Unchanged |
| `IPluginStateStore.cs` | Unchanged |
| `PluginLoadResult.cs` | Unchanged |

These 5 files are already **dependency-free** — they use only `System.*` namespaces. Move them as-is.

### Changes in main project

- Add `<ProjectReference Include="..\UGSGit.PluginAbstractions\UGSGit.PluginAbstractions.csproj" />` to `UGSGit.csproj`
- `git rm` the 5 files from `src/Models/` (they now live in the abstractions project)
- Update `using` statements throughout the codebase: no change needed since the `RootNamespace` is `UGSGit.Models`

### NuGet packaging (optional future)

Add to the abstractions csproj:
```xml
<PropertyGroup>
  <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  <PackageId>UGSGit.PluginAbstractions</PackageId>
  <Version>$(Version)</Version>
  <Description>Plugin contract interfaces for UGSGit plugins</Description>
</PropertyGroup>
```

External plugin authors can then:
```
dotnet add package UGSGit.PluginAbstractions
```
or reference the project directly if working in-tree.

### Risk

| Risk | Mitigation |
|---|---|
| Namespace `UGSGit.Models` is now split across two assemblies | `using UGSGit.Models;` works the same from either assembly. No code changes needed. |
| `PluginRegistry` and `PluginLoader` still in main project (they reference non-interface types) | Correct — they are **host-side** code, not plugin-side code. They should stay in the main project. |

---

## Phase 1: Extract HelloWorld Plugin (Proof of Concept)

HelloWorld is simple enough to be the test case for the entire pattern.

### New project

`src/plugins/HelloWorld/UGSGit.Plugins.HelloWorld.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>UGSGit.Plugins.HelloWorld</AssemblyName>
    <RootNamespace>UGSGit.ViewModels.Tabs</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\UGSGit.PluginAbstractions\UGSGit.PluginAbstractions.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.15" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
  </ItemGroup>
</Project>
```

### Files to move

| File | From | To |
|---|---|---|
| `HelloWorldPluginManifest.cs` | `src/ViewModels/Tabs/` | `src/plugins/HelloWorld/` |
| `HelloWorldTab.cs` | `src/ViewModels/Tabs/` | `src/plugins/HelloWorld/` |
| `HelloWorldBodyViewModel.cs` | `src/ViewModels/Tabs/` | `src/plugins/HelloWorld/` |
| `HelloWorldToolbarViewModel.cs` | `src/ViewModels/Tabs/` | `src/plugins/HelloWorld/` |
| Any HelloWorld-related AXAML views (if they exist) | `src/Views/Tabs/` | `src/plugins/HelloWorld/` |

### Dependencies to resolve

The only main-project dependency in HelloWorld currently is:

- `Application.Current?.Resources["Icons.TabHelloWorld"]` in `HelloWorldTab.cs`

**Solution**: Pass the icon resource key (string) via `PluginContext` or let the main project inject it. Best approach: add an `IconKey` property to `PluginContext` or let the tab return a string key and have the host resolve it.

Alternatively, keep `Avalonia` as a dependency since the host and plugins share the same Avalonia version. This is pragmatic — HelloWorld already has a `PackageReference` to Avalonia.

### Registration

Startup registration stays the same — direct code, no reflection:

```csharp
// In App.TryLaunchAsNormal():
PluginRegistry.Instance.RegisterBuiltInManifest(new HelloWorldPluginManifest());
```

The only change: the `using` statement and DLL reference are now resolved via `ProjectReference` instead of the same assembly.

### Build behavior

| Configuration | HelloWorld project | Result |
|---|---|---|
| `Debug` | Built as sub-project | DLL available for `plugins/` dir if desired |
| `Release` (AOT default) | Linked into native binary via ProjectReference | Included in `ugsgit` native binary |
| `Release` + `DisableAOT=true` | Built as sub-project, copied to output | Loaded via `PluginLoader.Discover()` if placed in `plugins/` |

---

> [!IMPORTANT]
> **Phase 2 is deferred to a separate spec.** The following is the architectural analysis informing that future spec. It is NOT part of the current deliverable (Phase 0 + Phase 1). The effort estimate here is the council's revised figure (12–20h), not the original spec's underestimate (4–8h).

## Phase 2 (Deferred): Extract UnrealSync Plugin (Complex — 12–20h estimated)

UnrealSync is deeply coupled to main-project services. Council analysis found **30+ coupling points across 6 layers** (not the 7–12 originally estimated).

### The Coupling Map

| Layer | Namespace | Types used (~13 models + 9 services) |
|---|---|---|
| Services | `UGSGit.Services` | `GitSyncService`, `ConfigService`, `BuildService`, `EditorLauncher`, `PublishService`, `BuildGraphService`, `EngineInfoService`, `EngineDetector`, `ProcessHelper` |
| Models | `UGSGit.Models` | `UProjectMeta`, `UgsConfig`, `UgsBuildStep`, `UgsPackageProfile`, `UgsWorkspaceState`, `BuildModes`, `BuildVersion`, `EngineInfo`, `PublishManifest`, `PublishProgress`, `PublishResult`, `PublishStatus`, `StageResult`, `UatCommandPreset` |
| Native | `UGSGit.Native` | `Native.OS.LogException()` |
| Converters | `UGSGit.Converters` | `ObjectConverters.IsNotNull` (XAML reference) |
| AOT JSON | `UGSGit.Models` (internal) | `UnrealSyncJsonContext` — 9 `[JsonSerializable]` attributes, consumed by `ConfigService`, `EngineInfoService`, `PublishService` |
| UX | `Avalonia.*` | File picker dialogs, `Dispatcher.UIThread`, `DynamicResource` references (~54 in UnrealSync AXAML views) |

#### Critical path: `UnrealSyncJsonContext`

This `internal partial class` registers 9 `[JsonSerializable(Type)]` attributes for types like `UgsConfig`, `UgsBuildStep`, etc. It is consumed by `ConfigService`, `EngineInfoService`, and `PublishService` from the main project.

**The constraint**: AOT JSON source generators require `[JsonSerializable]` attributes to be in the same assembly as the serialized types. Moving model types to `PluginAbstractions` without also moving the `JsonContext` breaks AOT serialization. Moving the `JsonContext` to `PluginAbstractions` requires:
- Making it `public` (currently `internal`)
- Adding `System.Text.Json` as a dependency to the "zero dependency" PluginAbstractions project — violating its zero-dependency constraint
- OR creating a second `UGSGit.UnrealSync.Serialization` project

**Three-resolution options for the JsonContext problem**:

| Option | Pro | Con |
|--------|-----|-----|
| A. Make JsonContext `public` in main project, add `System.Text.Json` to PluginAbstractions | Simple | Broke zero-dep constraint for ALL plugins |
| B. Create `UGSGit.UnrealSync.Serialization` project | Clean isolation | Project proliferation |
| C. Keep model types in main project, expose only interfaces in PluginAbstractions | Zero-dep preserved | UnrealSync still references main project; not a true standalone plugin |

**Council recommendation**: Option C is the pragmatic choice. Accept that UnrealSync remains an "in-tree hosted plugin" (ProjectReference to main project, not just abstractions). This is the most realistic path given the coupling depth.

### Phase 2a: Decouple Service Dependencies

#### Proposed service interfaces

```csharp
// IPluginLogger.cs — first and simplest extraction
public interface IPluginLogger
{
    void Log(string message);
    void LogError(string message, Exception ex);
}

// IProcessHelper.cs — for OS-level operations
public interface IProcessHelper
{
    int Run(string fileName, string arguments, string workingDir);
    // ... etc
}

// IGitService.cs — minimal API needed by plugins
public interface IGitService
{
    Task<string> GetCurrentBranchAsync(string repoPath, CancellationToken ct = default);
    Task<string> GetCurrentCommitAsync(string repoPath, CancellationToken ct = default);
    // ... etc
}

// IConfigService.cs
public interface IConfigService
{
    // Methods to read/write plugin-specific config
}
```

#### Recommended extraction ordering (hardest last)

1. `IPluginLogger` — trivial, zero dependencies
2. `IProcessHelper` — wraps `ProcessHelper` static methods
3. `IConfigService` — blocked by `UnrealSyncJsonContext` placement (see above)
4. `IGitService` — straightforward, wraps `GitSyncService`
5. Remaining service interfaces — each is a thin wrapper
6. `IEngineDetector` — 554 lines, OS-specific registry/INI logic. Hardest.

#### Host-side implementation

```csharp
public class HostPluginLogger : IPluginLogger
{
    public void Log(string message) => Native.OS.Log(message);
    public void LogError(string message, Exception ex) => Native.OS.LogException(ex);
}

// Registration in App.TryLaunchAsNormal():
PluginContext.Logger = new HostPluginLogger();
// or inject via DI/constructor
```

### Phase 2b: Move UnrealSync to Sub-Project

Once decoupled:

`src/plugins/UnrealSync/UGSGit.Plugins.UnrealSync.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>UGSGit.Plugins.UnrealSync</AssemblyName>
    <RootNamespace>UGSGit.ViewModels.Tabs.UnrealSync</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\UGSGit.PluginAbstractions\UGSGit.PluginAbstractions.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.15" />
    <PackageReference Include="Avalonia.Controls.DataGrid" Version="11.3.13" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
  </ItemGroup>
</Project>
```

### Files to move (9 ViewModels + 14 Views + AXAML)

| File type | Count | New location |
|---|---|---|
| CS ViewModels | 9 | `src/plugins/UnrealSync/` |
| CS Views (code-behind) | 7 | `src/plugins/UnrealSync/` |
| AXAML Views | 7 | `src/plugins/UnrealSync/` |

### Views: AXAML reference updates

All `.axaml` files reference `xmlns:vm="clr-something:UGSGit.ViewModels.Tabs.UnrealSync"`. Since the `RootNamespace` stays the same, the CLR namespace doesn't change — Avalonia resolves it across assembly boundaries automatically as long as the ProjectReference exists.

**However**, there are two cross-assembly XAML concerns:

1. **`xmlns:vm` may need `;assembly=` qualifier**: When VM types move to a different assembly than the AXAML file, Avalonia may not resolve `clr-namespace:` without an explicit `;assembly=UGSGit.Plugins.HelloWorld` suffix. This needs empirical verification in Phase 1.
2. **~54 `DynamicResource` references** in UnrealSync AXAML views depend on the main app's theme dictionary (`Brush.Background`, `Brush.FG1`–`FG4`, etc.). These resolve at runtime from `Application.Current.Resources` and work cross-assembly, but **design-time XAML preview will break** in the plugin project. This is a developer experience trade-off.

### Risk: XAML cross-assembly resolution

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| `x:Class` in AXAML fails for cross-assembly code-behind | Medium | High | Ensure `x:Class` matches the full namespace+class. Add `;assembly=` to xmlns if needed. **Empirically verify in Phase 1.** |
| `DynamicResource` references break at design time | Certain | Low (design-time only) | Runtime resolution works. Accept design-time limitation. |
| Compiled bindings break for cross-assembly VMs | Low | Medium | `AvaloniaUseCompiledBindingsByDefault` in plugin project. Same Avalonia version. |

---

## Phase 3: Update Solution File

`UGSGit.slnx` adds (current deliverable — Phase 0 + Phase 1 only):

```xml
<Folder Name="/src/">
  <Project Path="src/UGSGit.PluginAbstractions/UGSGit.PluginAbstractions.csproj" />
  <Project Path="src/plugins/HelloWorld/UGSGit.Plugins.HelloWorld.csproj" />
  <Project Path="src/UGSGit.csproj" />
</Folder>
```

When Phase 2 is undertaken, the UnrealSync project is added:

```xml
<Folder Name="/src/plugins/">
  <Project Path="src/plugins/HelloWorld/UGSGit.Plugins.HelloWorld.csproj" />
  <Project Path="src/plugins/UnrealSync/UGSGit.Plugins.UnrealSync.csproj" />
</Folder>
```

---

## Phase 4: External Plugin Author Experience

Once Phase 0 is done, external plugin authors can:

```xml
<!-- MyCustomPlugin.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <AssemblyName>MyCustomPlugin</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="UGSGit.PluginAbstractions" Version="2026.0.3" />
  </ItemGroup>
</Project>
```

```csharp
using UGSGit.Models;

public class MyPluginManifest : IPluginManifest
{
    public string PluginId => "my.custom-plugin";
    public string DisplayName => "My Plugin";
    public string Description => "Does something useful";
    public string Version => "1.0.0";
    public string Author => "Me";
    public bool IsGlobalByDefault => false;
    public int DefaultSortOrder => 900;

    public IReadOnlyList<IRepositoryTab> CreateTabs(PluginContext context)
    {
        return new[] { new MyTab(context.RepositoryPath) };
    }
}
```

Build → copy DLL to `<app>/plugins/MyCustomPlugin.dll` → works.

---

## Verification Checklist

### Build
- [ ] `dotnet build` succeeds (Debug)
- [ ] `dotnet build -c Release` succeeds (AOT with `DISABLE_PLUGINS`)
- [ ] `dotnet build -c Release -p:DisableAOT=true` succeeds
- [ ] `dotnet run --project src/UGSGit.csproj` launches correctly
- [ ] `dotnet publish -c Release -r win-x64` (AOT publish with all new projects) succeeds
- [ ] `grep -r "namespace SourceGit" src/` → zero results (no namespace regression)

### PluginAbstractions
- [ ] `UGSGit.PluginAbstractions.dll` has zero dependencies on Avalonia, CommunityToolkit, or UGSGit main
- [ ] All 5 interface/model files moved correctly
- [ ] Main project resolves `using UGSGit.Models;` from the new assembly
- [ ] `TrimmerRootAssembly Include="UGSGit.PluginAbstractions"` added to main csproj
- [ ] AOT linker does not strip PluginAbstractions types (verify with `dotnet publish -c Release -r win-x64`)

### HelloWorld extraction
- [ ] HelloWorld tab loads and displays correctly
- [ ] `Icons.TabHelloWorld` resolves (icon shows in tab bar)
- [ ] Toolbar renders
- [ ] `dotnet build src/plugins/HelloWorld/UGSGit.Plugins.HelloWorld.csproj` builds standalone
- [ ] Cross-assembly `DynamicResource` references resolve at runtime (test theme brushes)
- [ ] `xmlns:vm="clr-namespace:..."` resolution works without `;assembly=` qualifier (verify, add if needed)

### UnrealSync extraction (when complete — Phase 2 deferred)
- [ ] UnrealSync tab loads without errors
- [ ] `.uproject` detection works
- [ ] Engine path detection works
- [ ] Build configurations work
- [ ] Editor launch works
- [ ] No `FileNotFoundException` at runtime for types that moved assemblies
- [ ] `UnrealSyncJsonContext` placement resolved (see Phase 2a critical path above)
- [ ] AOT publish works with UnrealSync as sub-project

### External plugin API
- [ ] A new minimal plugin can be authored with only `UGSGit.PluginAbstractions` as a dependency
- [ ] It loads via `PluginLoader.Discover()` when placed in `plugins/`
- [ ] It appears in the plugin settings UI

### CI
- [ ] CI builds all projects (solution-level build)
- [ ] CI format check runs on new project files
- [ ] Release workflow produces correct artifacts
- [ ] Release workflow AOT publish succeeds

---

## Future Work (Out of Scope)

| Item | Rationale |
|---|---|
| Hot-reload of plugins at runtime | Requires `isCollectible: true` in `PluginLoadContext`. Breaking change to lifecycle. |
| Plugin marketplace / package feed | Product decision, not architecture |
| Plugin sandbox / security isolation | Each plugin already has its own `AssemblyLoadContext`. Further sandboxing is complex. |
| Plugin-specific localization files | Current system uses main project's Locales. Plugins could contribute their own AXAML resources. |
| Plugin-specific resource dictionaries (icons, styles) | Plugins can ship AXAML resource dictionaries now. Loading them is straightforward. |

---

## Effort Estimate

| Phase | Description | Initial estimate | Council-revised estimate |
|---|---|---|---|
| 0 | Extract PluginAbstractions | 1-2 hours | 1-2 hours ✅ |
| 1 | Extract HelloWorld + cross-assembly XAML verification | 1-2 hours | 1-2 hours ✅ |
| **0+1** | **Current deliverable (ship together)** | **2-4 hours** | **3-8 hours** |
| 2a | Decouple UnrealSync service dependencies | 4-8 hours | **12-20 hours** (2-3×) |
| 2b | Extract UnrealSync | 2-4 hours | 6-8 hours (includes AXAML fixes) |
| 3 | Update solution, CI, build scripts | 1 hour | 1-2 hours |
| 4 | Verification + regression testing | 2-4 hours | 4-6 hours |
| **Total (all phases)** | | **11-21 hours** | **23-40 hours** |

### Why Phase 2a was underestimated

Council analysis revealed 30+ coupling points vs. the original 7 estimated, including:

1. **13+ model types** to move/extract (not 5) — `UgsWorkspaceState`, `BuildModes`, `BuildVersion`, `EngineInfo`, `PublishManifest`, `PublishProgress`, `PublishResult`, `PublishStatus`, `StageResult`, `UatCommandPreset`, plus the original 5
2. **`UnrealSyncJsonContext` placement** — circular dependency between model types and AOT serialization. Requires a separate design decision before any extraction can begin
3. **`ProcessHelper`** — not originally listed, wraps OS-level operations
4. **`EngineDetector`** — 554 lines of OS-specific registry/INI logic, not a simple model type
5. **54 `DynamicResource` references** in AXAML — design-time preview breaks

### Recommendation

**Ship Phase 0 + Phase 1 as a single PR (3-8h).** This delivers ~80% of the spec's benefit. Phase 2 should be a separate spec with its own design pass, especially for the `UnrealSyncJsonContext` problem.

---

## Architectural Diagram (Post-Extraction)

```
┌─────────────────────────────────────────────────┐
│                  UGSGit.slnx                     │
│                                                   │
│  ┌──────────────────────────────────────┐        │
│  │         UGSGit (main app)            │        │
│  │  - App.axaml.cs (startup, plugin     │        │
│  │    registration, DI setup)           │        │
│  │  - PluginRegistry, PluginLoader,     │        │
│  │    PluginActivator                   │        │
│  │  - Services (host implementations    │        │
│  │    of IPluginService interfaces)     │        │
│  │  - Models (host-only types like      │        │
│  │    Commit, Repository, etc.)         │        │
│  └──────────────────┬───────────────────┘        │
│                     │ ProjectReference            │
│                     ▼                             │
│  ┌─────────────────────────────────────────┐     │
│  │   UGSGit.PluginAbstractions             │     │
│  │   (net10.0, zero dependencies)          │     │
│  │   - IPluginManifest                     │     │
│  │   - IRepositoryTab                      │     │
│  │   - PluginContext                       │     │
│  │   - IPluginStateStore                   │     │
│  │   - PluginLoadResult                    │     │
│  │   - IPluginLogger (new)                 │     │
│  │   - IGitService (new)                   │     │
│  │   - IConfigService (new)                │     │
│  │   - UProjectMeta (moved from Models)    │     │
│  └────────────┬──────────────┬──────────────┘     │
│               │              │                     │
│    ProjectRef │        ProjectRef                  │
│               ▼              ▼                     │
│  ┌─────────────────┐  ┌──────────────────┐        │
│  │ HelloWorld       │  │ UnrealSync       │        │
│  │ Plugin           │  │ Plugin           │        │
│  │ (net10.0,       │  │ (net10.0,        │        │
│  │  +Avalonia,     │  │  +Avalonia,       │        │
│  │  +CommunityTk)  │  │  +CommunityTk,   │        │
│  └─────────────────┘  │  +DataGrid)       │        │
│                        └──────────────────┘        │
│                                                   │
│  External plugins (runtime load):                  │
│  ┌─────────────────┐                               │
│  │ ThirdParty.dll   │── AssemblyLoadContext ──→    │
│  │ (refs NuGet      │    PluginLoader.Discover()   │
│  │  abstractions)   │                              │
│  └─────────────────┘                               │
└─────────────────────────────────────────────────────┘

Dependency direction: main → abstractions ← plugins (one-way)
No circular dependencies at any level.
```

---

## Key Design Decisions

| Decision | Rationale |
|---|---|
| **`PluginAbstractions` uses `UGSGit.Models` namespace** | Zero code changes in main project. `using UGSGit.Models;` works from any assembly. |
| **Keep `PluginRegistry`, `PluginLoader`, `PluginActivator` in main project** | These are **host** infrastructure, not plugin contract. External plugins never need them. |
| **HelloWorld keeps Avalonia as a NuGet dependency** | It needs `Application.Current?.Resources`. Adding an indirection layer for this single usage is over-engineering. |
| **UnrealSync service interfaces in `PluginAbstractions`** (Phase 2) | Breaking the coupling requires interfaces. `PluginAbstractions` is the natural home. |
| **Compiled bindings enabled in plugin projects** | Ensures AOT-compatible XAML when building plugins standalone. Same as main project. |
| **No DI container** | The current pattern (static `PluginRegistry.Instance`, direct constructor injection in tabs) is simple and works. No need for a container. |
| **`RootNamespace` matches original** | No namespace changes = no search-and-replace = no merge conflicts when pulling upstream. |
| **Phase 2 deferred to separate spec** | Council found 2-3× effort underestimate (30+ coupling points vs. 7). Focus on Phase 0+1 which delivers ~80% of benefit. |
| **`TrimmerRootAssembly` for each new project in AOT builds** | Prevents AOT linker from stripping types needed by new assemblies. Mirror existing pattern for `UGSGit` and `Avalonia.Themes.Fluent`. |
| **UnrealSyncJsonContext resolution deferred** | Three options identified (make public, create serialization project, or keep models in main project). Decision requires deeper design pass. |

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| UnrealSync service coupling too deep to extract practically (Phase 2) | Medium | High | Keep UnrealSync as an in-tree plugin. Accept tight coupling. Deferred to separate spec. |
| XAML cross-assembly resolution breaks at runtime (`x:Class` with `;assembly=` needed) | Medium | High | Test with HelloWorld in Phase 1. Add `;assembly=` qualifier to `xmlns:vm` if needed. Empirical fix. |
| AOT linker strips types needed by new assemblies | Medium | High | Add `TrimmerRootAssembly Include="UGSGit.PluginAbstractions"` to main csproj. Also need entries for any plugin projects linked via ProjectReference. Test with `dotnet publish -c Release -r win-x64`. |
| `UnrealSyncJsonContext` circular dependency blocks Phase 2a | High (Phase 2) | High | Resolve via one of 3 options: make JsonContext public, create serialization project, or keep models in main project. Deferred to separate spec. |
| Plugin registration timing — abstractions assembly not loaded | Low | Medium | ProjectReference ensures it's loaded at startup. External plugins load own copy via AssemblyLoadContext — collision detection handles this. |
| `dotnet format` and editorconfig rules don't cover plugin projects | Low | Low | Ensure `.editorconfig` applies to all `src/` subdirectories. Add `Directory.Build.props` if needed. |
| NuGet package version drift between main project and abstractions | Low | Low | PluginAbstractions NuGet version matches main app version. CI publishes both. |
| Design-time XAML preview breaks for plugin project AXAML files | Certain | Low (DX only) | Runtime resolution works. Accept design-time limitation. Add note to developer docs. |
| CI workflows not updated for multi-project solution | Medium | Medium | Update `.github/workflows/*.yml` to include all projects. Add `dotnet restore` at solution level. |
| `xmlns:vm` resolution without `;assembly=` creates a bootstrap issue (Phase 1 must check this) | Medium | Medium | Phase 1 is explicitly tasked with empirical verification. If it fails, the fix is mechanical (find-replace in AXAML files). |
