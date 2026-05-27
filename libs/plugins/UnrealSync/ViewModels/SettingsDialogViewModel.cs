#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using UGSGit.PluginAbstractions;

namespace UGSGit.Plugins.UnrealSync.ViewModels;

/// <summary>
/// ViewModel for the UnrealSync Settings dialog.
/// Engine path override is saved to user-local config (.unrealsync/local-ue-path.json).
/// All other settings are saved to team-shared config (.unrealsync-settings.json).
/// Fixes D-1: engine path is user-local, not team-shared.
/// </summary>
public partial class SettingsDialogViewModel : ObservableObject
{
    private readonly string _repoPath;
    private readonly string _enginePath;
    private readonly string _uprojectPath;
    private readonly IConfigService _configService;

    /// <summary>Project name derived from the .uproject filename, used for variable expansion hints.</summary>
    public string ProjectName
    {
        get
        {
            return !string.IsNullOrEmpty(_uprojectPath)
                ? Path.GetFileNameWithoutExtension(_uprojectPath)
                : "Project";
        }
    }

    /// <summary>Detected engine root directory path.</summary>
    public string EnginePath => _enginePath;

    // Engine — saved to LOCAL config (fixes D-1)
    /// <summary>User-specified override path to the Unreal Engine root directory.</summary>
    [ObservableProperty] private string _enginePathOverride = string.Empty;
    /// <summary>Whether to auto-detect the engine path from the .uproject association.</summary>
    [ObservableProperty] private bool _autoDetectEngine = true;

    // Network — saved to SHARED config
    /// <summary>Base URL or UNC path for network publish destination.</summary>
    [ObservableProperty] private string _networkBaseUrl = string.Empty;
    /// <summary>Channel name for editor builds (subdirectory under network base).</summary>
    [ObservableProperty] private string _editorChannel = "Editor";
    /// <summary>Channel name for game builds (subdirectory under network base).</summary>
    [ObservableProperty] private string _gameChannel = "Game";
    /// <summary>Base name for published/downloaded zip files. Empty = use .uproject name.</summary>
    [ObservableProperty] private string _binaryName = string.Empty;
    /// <summary>Hex color for editor build badges. Empty = default green.</summary>
    [ObservableProperty] private string _editorBadgeColor = string.Empty;
    /// <summary>Hex color for game build badges. Empty = default orange.</summary>
    [ObservableProperty] private string _gameBadgeColor = string.Empty;
    /// <summary>Hex color for commit-code badges. Empty = theme default.</summary>
    [ObservableProperty] private string _commitCodeBadgeColor = string.Empty;
    /// <summary>Hex color for commit-content badges. Empty = theme default.</summary>
    [ObservableProperty] private string _commitContentBadgeColor = string.Empty;

    // BuildGraph Scripts — saved to SHARED config
    /// <summary>Path to the editor BuildGraph XML script, relative to engine root.</summary>
    [ObservableProperty] private string _editorBuildGraphScript = "";
    /// <summary>BuildGraph aggregate target name for the editor script.</summary>
    [ObservableProperty] private string _editorBuildGraphTarget = "";
    /// <summary>Path to the game BuildGraph XML script, relative to engine root.</summary>
    [ObservableProperty] private string _gameBuildGraphScript = "";
    /// <summary>BuildGraph aggregate target name for the game script.</summary>
    [ObservableProperty] private string _gameBuildGraphTarget = "";
    /// <summary>Path to the server BuildGraph XML script, relative to engine root.</summary>
    [ObservableProperty] private string _serverBuildGraphScript = "";
    /// <summary>BuildGraph aggregate target name for the server script.</summary>
    [ObservableProperty] private string _serverBuildGraphTarget = "";
    /// <summary>Template for -set: arguments passed to BuildGraph (supports {UbtTarget}, {ProjectPath}, {ShortSha}, {ProjectName}).</summary>
    [ObservableProperty] private string _buildGraphSetArgsTemplate = "";

    // Performance — saved to SHARED config
    /// <summary>Maximum number of concurrent git.exe processes for commit type annotation (1–20).</summary>
    [ObservableProperty] private int _maxConcurrentGitProcesses = UgsConfig.DefaultMaxConcurrentGitProcesses;

    /// <summary>Preset hex colors for the color picker flyout.</summary>
    public List<string> PresetColors { get; } = new()
    {
        "#00FF00", "#32CD32", "#228B22", "#006400",  // Greens
        "#FFA500", "#FF8C00", "#FF4500", "#FF6347",  // Oranges/Reds
        "#1E90FF", "#00BFFF", "#4169E1", "#0000FF",  // Blues
        "#FFD700", "#FFFF00", "#F0E68C", "#B8860B",  // Yellows
        "#FF69B4", "#FF1493", "#C71585", "#800080",  // Pinks/Purples
        "#00CED1", "#20B2AA", "#008B8B", "#5F9EA0",  // Teals
        "#A9A9A9", "#808080", "#696969", "#000000",  // Grays/Black
        "#FFFFFF", "#F5F5F5", "#D3D3D3", "#C0C0C0",  // Whites
    };

    // Build defaults — saved to SHARED config
    /// <summary>Output directory for staged builds relative to the repository root.</summary>
    [ObservableProperty] private string _outputDirectory = "Saved/StagedBuilds";

    // Publish options — saved to SHARED config
    /// <summary>Whether to use atomic publish (swap directory atomically on the network share).</summary>
    [ObservableProperty] private bool _atomicPublish = true;

    // Build Targets — saved to SHARED config
    /// <summary>Observable collection of mutable build target edit models.</summary>
    public ObservableCollection<BuildTargetEditModel> BuildTargets { get; } = new();

    /// <summary>Available UAT command presets for the dropdown.</summary>
    public List<UatCommandPreset> UatPresetList => UatCommandPresets.All;

    /// <summary>Display strings for build mode dropdown (UBT Build, UAT Build, Custom Script).</summary>
    public List<string> BuildModeOptions { get; } = new() { "UBT Build", "UAT Build", "Custom Script" };

    // Validation errors (fixes H-4)
    /// <summary>Validation error message for the engine path field.</summary>
    [ObservableProperty] private string _enginePathError = string.Empty;
    /// <summary>Validation error message for the network URL field.</summary>
    [ObservableProperty] private string _networkUrlError = string.Empty;
    /// <summary>Validation error message for the max concurrent git processes field.</summary>
    [ObservableProperty] private string _maxConcurrentGitProcessesError = string.Empty;

    /// <summary>
    /// Available template variables that can be used in build target fields.
    /// </summary>
    public record BuildVariable(string Name, string Description);

    public ObservableCollection<BuildVariable> AvailableVariables { get; } = new()
    {
        new BuildVariable("{ProjectName}", "The .uproject filename without extension (e.g. MyProject)."),
        new BuildVariable("{EnginePath}", "The detected engine root directory (e.g., C:\\Program Files\\Epic Games\\UE_5.4)."),
        new BuildVariable("{ProjectPath}", "Full path to the .uproject file (e.g., C:\\Projects\\MyProject\\MyProject.uproject)."),
        new BuildVariable("{Target}", "The resolved UBT target name (e.g., MyProjectEditor or MyProject)."),
        new BuildVariable("{UbtTarget}", "The resolved UBT target name, alias for {Target} (e.g., MyProjectEditor)."),
        new BuildVariable("{Platform}", "The build platform (e.g., Win64, Linux, Mac, Android, IOS)."),
        new BuildVariable("{Configuration}", "The build configuration (e.g., Development, DebugGame, Shipping, Test)."),
        new BuildVariable("{ArchiveDir}", "The configured output/staging directory (e.g., Saved/StagedBuilds). Resolved from BuildDefaults.OutputDirectory."),
    };

    /// <summary>
    /// Mutable edit model for build target steps, since UgsBuildStep is an immutable record.
    /// Supports BuildMode (UBT/UAT/Custom) with per-field dirty tracking for mode switching.
    /// </summary>
    /// <summary>Mutable edit model for build target configuration.</summary>
    public partial class BuildTargetEditModel : ObservableObject
    {
        /// <summary>Unique identifier for this build target.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

        /// <summary>User-visible name for this build target.</summary>
        public string DisplayName { get; set; } = "New Target";

        // Build mode — determines default ScriptPath and Arguments
        /// <summary>Build mode string (UBT Build, UAT Build, or Custom Script).</summary>
        [ObservableProperty] private string _buildMode = BuildModes.Ubt;
        /// <summary>UAT command preset name used when BuildMode is UAT.</summary>
        [ObservableProperty] private string _uatCommand = "BuildCookRun";

        // Target type — curated dropdown mapping to UBT target names
        /// <summary>Curated build type (Editor, Game, Client, Server) used to derive the UBT target string.</summary>
        [ObservableProperty] private string _buildType = "Editor";

        // Raw UBT target string, stored in config for backward compat
        /// <summary>Raw UBT target string (e.g. {ProjectName}Editor).</summary>
        public string Target { get; set; } = "{ProjectName}Editor";

        /// <summary>Target platform for the build (e.g. Win64, Linux, Android).</summary>
        [ObservableProperty] private string _platform = "Win64";

        /// <summary>Build configuration (e.g. Development, Shipping, DebugGame).</summary>
        [ObservableProperty] private string _configuration = "Development";

        /// <summary>Path to the build script or executable invoked for this target.</summary>
        [ObservableProperty] private string _scriptPath = "";

        /// <summary>Command-line arguments passed to the build script.</summary>
        [ObservableProperty] private string _arguments = "";

        // Dirty tracking — prevents overwriting user edits on mode switches
        private bool _scriptPathUserModified;
        private bool _argumentsUserModified;
        private bool _buildTypeUserModified;
        private bool _suppressDirtyTracking;

        /// <summary>Sort position for execution order among build targets.</summary>
        public int OrderIndex { get; set; }

        /// <summary>Whether to run this target on normal (manual) sync.</summary>
        public bool RunOnNormalSync { get; set; } = true;

        /// <summary>Whether to run this target on scheduled (automatic) sync.</summary>
        public bool RunOnScheduledSync { get; set; } = false;

        // Whether UatCommand dropdown should be visible
        /// <summary>Whether the UAT command dropdown should be visible (true when BuildMode is UAT).</summary>
        public bool ShowUatCommand => BuildMode == BuildModes.Uat;

        partial void OnBuildModeChanged(string value)
        {
            OnPropertyChanged(nameof(ShowUatCommand));
            ApplyDefaults(resetDirty: false);
        }

        partial void OnUatCommandChanged(string value)
        {
            if (BuildMode != BuildModes.Uat) return;

            // Recompute arguments from preset if not user-modified
            if (!_argumentsUserModified || string.IsNullOrEmpty(Arguments))
            {
                _suppressDirtyTracking = true;
                try
                {
                    var preset = UatCommandPresets.Find(UatCommand);
                    Arguments = preset?.ArgumentsTemplate ?? "";
                    _argumentsUserModified = false;
                }
                finally
                {
                    _suppressDirtyTracking = false;
                }
            }

            // Auto-adjust BuildType if user hasn't manually changed it
            var presetForType = UatCommandPresets.Find(UatCommand);
            if (presetForType?.AutoBuildType is { Length: > 0 } && !_buildTypeUserModified)
            {
                _suppressDirtyTracking = true;
                try { BuildType = presetForType.AutoBuildType; }
                finally { _suppressDirtyTracking = false; }
            }
        }

        partial void OnScriptPathChanged(string value)
        {
            if (!_suppressDirtyTracking) _scriptPathUserModified = true;
        }

        partial void OnArgumentsChanged(string value)
        {
            if (!_suppressDirtyTracking) _argumentsUserModified = true;
        }

        partial void OnBuildTypeChanged(string value)
        {
            if (!_suppressDirtyTracking) _buildTypeUserModified = true;

            // In UBT mode, recompute Arguments if not user-modified
            if (BuildMode == BuildModes.Ubt && !_argumentsUserModified)
            {
                _suppressDirtyTracking = true;
                try
                {
                    var defaults = ComputeDefaults(BuildModes.Ubt, null!);
                    Arguments = defaults.Arguments;
                }
                finally
                {
                    _suppressDirtyTracking = false;
                }
            }
        }

        /// <summary>
        /// Applies mode-appropriate defaults to ScriptPath and Arguments.
        /// If resetDirty is true, clears dirty flags (used on initial load or after explicit
        /// "Reset to defaults" action).
        /// </summary>
        /// <param name="resetDirty">
        /// If true, resets the user-modified tracking flags so defaults will be reapplied
        /// on subsequent mode switches.
        /// </param>
        public void ApplyDefaults(bool resetDirty = false)
        {
            var defaults = ComputeDefaults(BuildMode, UatCommand);

            _suppressDirtyTracking = true;
            try
            {
                if (!_scriptPathUserModified || string.IsNullOrEmpty(ScriptPath) || resetDirty)
                {
                    ScriptPath = defaults.ScriptPath;
                    if (resetDirty) _scriptPathUserModified = false;
                }

                if (!_argumentsUserModified || string.IsNullOrEmpty(Arguments) || resetDirty)
                {
                    Arguments = defaults.Arguments;
                    if (resetDirty) _argumentsUserModified = false;
                }
            }
            finally
            {
                _suppressDirtyTracking = false;
            }
        }

        /// <summary>
        /// Computes platform-aware defaults for ScriptPath and Arguments based on the
        /// given build mode and UAT command.
        /// </summary>
        /// <param name="buildMode">The build mode string (UBT, UAT, or Custom).</param>
        /// <param name="uatCommand">The UAT command preset name; used only when buildMode is UAT.</param>
        /// <returns>
        /// A tuple containing the default ScriptPath and Arguments strings.
        /// For Custom mode, both values are empty strings.
        /// </returns>
        public (string ScriptPath, string Arguments) ComputeDefaults(string buildMode, string? uatCommand)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var scriptExt = isWindows ? ".bat" : ".sh";

            return buildMode switch
            {
                BuildModes.Ubt => (
                    ScriptPath: $"{{EnginePath}}/Engine/Build/BatchFiles/Build{scriptExt}",
                    Arguments: $"-Target=\"{{UbtTarget}} {{Platform}} {{Configuration}} -Project=\\\"{{ProjectPath}}\\\"\" -WaitMutex"
                ),
                BuildModes.Uat => ComputeUatDefaults(uatCommand, isWindows),
                BuildModes.Custom => (ScriptPath: "", Arguments: ""),
                _ => (ScriptPath: "", Arguments: "")
            };
        }

        private (string ScriptPath, string Arguments) ComputeUatDefaults(string? uatCommand, bool isWindows)
        {
            var scriptExt = isWindows ? ".bat" : ".sh";
            var scriptPath = $"{{EnginePath}}/Engine/Build/BatchFiles/RunUAT{scriptExt}";

            var preset = UatCommandPresets.Find(uatCommand);
            if (preset == null)
                return (scriptPath, "");

            return (scriptPath, preset.ArgumentsTemplate);
        }

        /// <summary>
        /// Converts this edit model to an immutable UgsBuildStep record for persistence.
        /// The UBT target string is derived from the curated BuildType.
        /// </summary>
        /// <returns>An immutable UgsBuildStep record with the current model values.</returns>
        public UgsBuildStep ToStep()
        {
            var ubtTarget = BuildType switch
            {
                "Game"   => "{ProjectName}",
                "Client" => "{ProjectName}Client",
                "Editor" => "{ProjectName}Editor",
                "Server" => "{ProjectName}Server",
                _        => Target ?? "{ProjectName}Editor",
            };
            return new UgsBuildStep(
                Id, DisplayName, ubtTarget, Platform, Configuration,
                ScriptPath, Arguments,
                OrderIndex, RunOnNormalSync, RunOnScheduledSync,
                BuildMode, UatCommand    // new trailing params
            );
        }

        /// <summary>
        /// Creates a BuildTargetEditModel from an immutable UgsBuildStep record,
        /// preserving backward compatibility for missing BuildMode/UatCommand fields.
        /// </summary>
        /// <param name="s">The source UgsBuildStep record.</param>
        /// <returns>A new BuildTargetEditModel populated from the record values.</returns>
        public static BuildTargetEditModel FromStep(UgsBuildStep s)
        {
            var model = new BuildTargetEditModel
            {
                Id = s.Id,
                DisplayName = s.DisplayName,
                Target = s.Target,
                BuildType = InferBuildType(s.Target),
                Platform = s.Platform,
                Configuration = s.Configuration,
                ScriptPath = s.ScriptPath,
                Arguments = s.Arguments,
                BuildMode = string.IsNullOrEmpty(s.BuildMode) ? BuildModes.Ubt : s.BuildMode,   // backward compat
                UatCommand = s.UatCommand ?? "BuildCookRun",                                      // backward compat
                OrderIndex = s.OrderIndex,
                RunOnNormalSync = s.RunOnNormalSync,
                RunOnScheduledSync = s.RunOnScheduledSync
                // dirty flags default to false — fields are considered "not user-modified" on load
            };

            return model;
        }

        /// <summary>
        /// Infer the curated BuildType from a raw UBT target string.
        /// Handles both templated ({ProjectName}Editor) and literal (MyProjectEditor) targets.
        /// </summary>
        private static string InferBuildType(string target)
        {
            if (string.IsNullOrEmpty(target)) return "Editor";
            var suffix = target.Replace("{ProjectName}", "", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(suffix)) return "Game";
            if (suffix.EndsWith("Editor", StringComparison.OrdinalIgnoreCase)) return "Editor";
            if (suffix.EndsWith("Client", StringComparison.OrdinalIgnoreCase)) return "Client";
            if (suffix.EndsWith("Server", StringComparison.OrdinalIgnoreCase)) return "Server";
            return "Editor";
        }
    }

    /// <summary>
    /// Initializes a new instance of the SettingsDialogViewModel with the specified repository and service context.
    /// </summary>
    /// <param name="repoPath">Absolute path to the Git repository root.</param>
    /// <param name="enginePath">Absolute path to the Unreal Engine root directory.</param>
    /// <param name="uprojectPath">Absolute path to the .uproject file.</param>
    /// <param name="configService">Service for loading and saving UnrealSync config files.</param>
    public SettingsDialogViewModel(string repoPath, string enginePath, string uprojectPath, IConfigService configService)
    {
        _repoPath = repoPath;
        _enginePath = enginePath;
        _uprojectPath = uprojectPath;
        _configService = configService;
        LoadFromConfig();
    }

    /// <summary>
    /// Load current config values into the VM properties.
    /// </summary>
    private void LoadFromConfig()
    {
        var config = _configService.LoadConfig(_repoPath);
        var localState = _configService.LoadLocalState(_repoPath);

        // Engine (local)
        EnginePathOverride = localState.EnginePathOverride;
        AutoDetectEngine = config.Engine?.AutoDetect ?? true;

        // Network (shared)
        NetworkBaseUrl = config.NetworkBase;
        EditorChannel = config.EditorChannel;
        GameChannel = config.GameChannel;
        BinaryName = config.BinaryName;
        EditorBadgeColor = config.EditorBadgeColor;
        GameBadgeColor = config.GameBadgeColor;
        CommitCodeBadgeColor = config.CommitCodeBadgeColor;
        CommitContentBadgeColor = config.CommitContentBadgeColor;
        MaxConcurrentGitProcesses = config.MaxConcurrentGitProcesses > 0
            ? config.MaxConcurrentGitProcesses
            : UgsConfig.DefaultMaxConcurrentGitProcesses;

        // Build defaults (shared)
        OutputDirectory = config.BuildDefaults?.OutputDirectory ?? "Saved/StagedBuilds";

        // Publish (shared)
        AtomicPublish = config.Publish?.Atomic ?? true;

        // BuildGraph scripts (shared)
        var bg = config.BuildGraph ?? new UgsBuildGraphConfig();
        EditorBuildGraphScript = bg.EditorScript;
        EditorBuildGraphTarget = bg.EditorTarget;
        GameBuildGraphScript = bg.GameScript;
        GameBuildGraphTarget = bg.GameTarget;
        ServerBuildGraphScript = bg.ServerScript;
        ServerBuildGraphTarget = bg.ServerTarget;
        BuildGraphSetArgsTemplate = bg.SetArgsTemplate;

        // Build targets (shared) — already migrated by ConfigService
        BuildTargets.Clear();
        foreach (var step in config.Engine?.BuildTargets ?? new())
        {
            BuildTargets.Add(BuildTargetEditModel.FromStep(step));
        }
    }

    /// <summary>Validates and persists settings to local + shared config.</summary>
    [RelayCommand]
    private void Save()
    {
        if (!Validate()) return;

        var sharedConfig = _configService.LoadConfig(_repoPath);
        var localState = _configService.LoadLocalState(_repoPath);

        // Update shared config — use with expressions since config types are now immutable records (fixes M-2)
        // Match current config model version (defaults to 4 with BuildGraph fields)
        sharedConfig = sharedConfig with
        {
            Version = 4,
            NetworkBase = NetworkBaseUrl,
            Engine = (sharedConfig.Engine ?? new UgsEngineConfig()) with
            {
                BuildTargets = new List<UgsBuildStep>(BuildTargets.Select(x => x.ToStep())),
                AutoDetect = AutoDetectEngine
            },
            EditorChannel = EditorChannel,
            GameChannel = GameChannel,
            BinaryName = BinaryName,
            EditorBadgeColor = EditorBadgeColor,
            GameBadgeColor = GameBadgeColor,
            CommitCodeBadgeColor = CommitCodeBadgeColor,
            CommitContentBadgeColor = CommitContentBadgeColor,
            MaxConcurrentGitProcesses = MaxConcurrentGitProcesses,
        };

        if (sharedConfig.BuildDefaults != null)
        {
            sharedConfig = sharedConfig with
            {
                BuildDefaults = sharedConfig.BuildDefaults with
                {
                    OutputDirectory = OutputDirectory,
                }
            };
        }

        if (sharedConfig.Publish != null)
        {
            sharedConfig = sharedConfig with
            {
                Publish = sharedConfig.Publish with
                {
                    Atomic = AtomicPublish,
                }
            };
        }

        // BuildGraph scripts
        sharedConfig = sharedConfig with
        {
            BuildGraph = new UgsBuildGraphConfig
            {
                EditorScript = EditorBuildGraphScript,
                EditorTarget = EditorBuildGraphTarget,
                GameScript = GameBuildGraphScript,
                GameTarget = GameBuildGraphTarget,
                ServerScript = ServerBuildGraphScript,
                ServerTarget = ServerBuildGraphTarget,
                SetArgsTemplate = BuildGraphSetArgsTemplate,
            }
        };

        _configService.SaveConfig(_repoPath, sharedConfig);

        // Propagate new concurrency limit to process-wide throttle
        var effectiveMax = sharedConfig.MaxConcurrentGitProcesses > 0
            ? sharedConfig.MaxConcurrentGitProcesses
            : UgsConfig.DefaultMaxConcurrentGitProcesses;
        GitProcessLimiter.UpdateMaxConcurrency(effectiveMax);

        // Update local config (fixes D-1: engine path is user-local)
        localState.EnginePathOverride = EnginePathOverride;
        _configService.SaveLocalState(_repoPath, localState);
    }

    /// <summary>
    /// True when all three BuildGraph script fields are empty, indicating the warning should be shown.
    /// </summary>
    public bool HasNoBuildGraphScripts =>
        string.IsNullOrWhiteSpace(EditorBuildGraphScript)
        && string.IsNullOrWhiteSpace(GameBuildGraphScript)
        && string.IsNullOrWhiteSpace(ServerBuildGraphScript);

    // Notify HasNoBuildGraphScripts when any script field changes
    partial void OnEditorBuildGraphScriptChanged(string value) => OnPropertyChanged(nameof(HasNoBuildGraphScripts));
    partial void OnGameBuildGraphScriptChanged(string value) => OnPropertyChanged(nameof(HasNoBuildGraphScripts));
    partial void OnServerBuildGraphScriptChanged(string value) => OnPropertyChanged(nameof(HasNoBuildGraphScripts));

    /// <summary>Sets the editor badge color from a preset swatch.</summary>
    [RelayCommand]
    private void SetEditorColor(string color) => EditorBadgeColor = color;

    /// <summary>Sets the game badge color from a preset swatch.</summary>
    [RelayCommand]
    private void SetGameColor(string color) => GameBadgeColor = color;

    /// <summary>Sets the commit-code badge color from a preset swatch.</summary>
    [RelayCommand]
    private void SetCommitCodeColor(string color) => CommitCodeBadgeColor = color;

    /// <summary>Sets the commit-content badge color from a preset swatch.</summary>
    [RelayCommand]
    private void SetCommitContentColor(string color) => CommitContentBadgeColor = color;

    /// <summary>Fired when the dialog should close (Cancel button pressed).</summary>
    public event Action? RequestClose;

    /// <summary>Discards changes and requests dialog close.</summary>
    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    /// <summary>Adds a new build target with default values.</summary>
    [RelayCommand]
    private void AddBuildTarget()
    {
        var newTarget = new BuildTargetEditModel();
        newTarget.ApplyDefaults(resetDirty: true);  // Pre-populate defaults for UBT mode
        BuildTargets.Add(newTarget);
    }

    /// <summary>Removes the specified build target from the collection.</summary>
    /// <param name="step">The build target edit model to remove.</param>
    [RelayCommand]
    private void RemoveBuildTarget(BuildTargetEditModel step)
    {
        BuildTargets.Remove(step);
    }

    /// <summary>
    /// Validates engine path and network URL before saving.
    /// Returns false if any field is invalid and sets the corresponding error message.
    /// </summary>
    /// <returns>True if all settings are valid; false otherwise.</returns>
    private bool Validate()
    {
        var valid = true;
        EnginePathError = string.Empty;
        NetworkUrlError = string.Empty;
        MaxConcurrentGitProcessesError = string.Empty;

        if (!AutoDetectEngine && string.IsNullOrWhiteSpace(EnginePathOverride))
        {
            EnginePathError = "Engine path is required when auto-detect is disabled";
            valid = false;
        }
        else if (!string.IsNullOrWhiteSpace(EnginePathOverride) &&
                 !Directory.Exists(Path.Combine(EnginePathOverride, "Engine", "Binaries")))
        {
            EnginePathError = "Engine path does not exist or is invalid";
            valid = false;
        }

        if (!string.IsNullOrWhiteSpace(NetworkBaseUrl) &&
            !Regex.IsMatch(NetworkBaseUrl, @"^(\\\\|https?://)"))
        {
            NetworkUrlError = "Base URL must be a valid UNC path or HTTP URL";
            valid = false;
        }

        if (MaxConcurrentGitProcesses < 1 || MaxConcurrentGitProcesses > 20)
        {
            MaxConcurrentGitProcessesError = "Must be between 1 and 20";
            valid = false;
        }

        return valid;
    }

}
