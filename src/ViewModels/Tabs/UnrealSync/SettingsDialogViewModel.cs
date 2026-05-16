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

using SourceGit.Models;
using SourceGit.Services;

namespace SourceGit.ViewModels.Tabs.UnrealSync;

/// <summary>
/// ViewModel for the UnrealSync Settings dialog.
/// Engine path override is saved to user-local config (.unrealsync/local.json).
/// All other settings are saved to team-shared config (.unrealsync.json).
/// Fixes D-1: engine path is user-local, not team-shared.
/// </summary>
public partial class SettingsDialogViewModel : ObservableObject
{
    private readonly string _repoPath;
    private readonly string _enginePath;
    private readonly string _uprojectPath;

    // Derived from the selected .uproject — used for variable expansion hints
    public string ProjectName
    {
        get
        {
            return !string.IsNullOrEmpty(_uprojectPath)
                ? Path.GetFileNameWithoutExtension(_uprojectPath)
                : "Project";
        }
    }

    public string EnginePath => _enginePath;

    // Engine — saved to LOCAL config (fixes D-1)
    [ObservableProperty] private string _enginePathOverride = string.Empty;
    [ObservableProperty] private bool _autoDetectEngine = true;

    // Network — saved to SHARED config
    [ObservableProperty] private string _networkBaseUrl = string.Empty;
    [ObservableProperty] private string _archiveChannel = "Editor";
    [ObservableProperty] private string _publishChannel = "Editor";
    [ObservableProperty] private string _customArchiveChannel = string.Empty;
    [ObservableProperty] private string _customPublishChannel = string.Empty;

    // Build defaults — saved to SHARED config
    [ObservableProperty] private string _defaultBuildConfig = "Development";
    [ObservableProperty] private bool _buildContentWhenPackaging;
    [ObservableProperty] private string _outputDirectory = "Saved/StagedBuilds";

    // Publish options — saved to SHARED config
    [ObservableProperty] private bool _atomicPublish = true;

    // Build Targets — saved to SHARED config
    public ObservableCollection<BuildTargetEditModel> BuildTargets { get; } = new();

    // UAT command presets for the dropdown
    public List<UatCommandPreset> UatPresetList => UatCommandPresets.All;

    // Build mode options for the dropdown
    public List<string> BuildModeOptions { get; } = new() { "UBT Build", "UAT Build", "Custom Script" };

    // Validation errors (fixes H-4)
    [ObservableProperty] private string _enginePathError = string.Empty;
    [ObservableProperty] private string _networkUrlError = string.Empty;

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
    public partial class BuildTargetEditModel : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string DisplayName { get; set; } = "New Target";

        // Build mode — determines default ScriptPath and Arguments
        [ObservableProperty] private string _buildMode = BuildModes.Ubt;
        [ObservableProperty] private string _uatCommand = "BuildCookRun";

        // Target type — curated dropdown mapping to UBT target names
        [ObservableProperty] private string _buildType = "Editor";

        // Raw UBT target string, stored in config for backward compat
        public string Target { get; set; } = "{ProjectName}Editor";

        [ObservableProperty] private string _platform = "Win64";
        [ObservableProperty] private string _configuration = "Development";

        [ObservableProperty] private string _scriptPath = "";
        [ObservableProperty] private string _arguments = "";

        // Dirty tracking — prevents overwriting user edits on mode switches
        private bool _scriptPathUserModified;
        private bool _argumentsUserModified;
        private bool _buildTypeUserModified;
        private bool _suppressDirtyTracking;

        public int OrderIndex { get; set; }
        public bool RunOnNormalSync { get; set; } = true;
        public bool RunOnScheduledSync { get; set; } = false;

        // Whether UatCommand dropdown should be visible
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
        /// Apply mode-appropriate defaults. If resetDirty is true, clears dirty flags
        /// (used on initial load or after explicit "Reset to defaults" action).
        /// </summary>
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
        /// Compute platform-aware defaults for the given build mode and UAT command.
        /// </summary>
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

    public SettingsDialogViewModel(string repoPath, string enginePath, string uprojectPath)
    {
        _repoPath = repoPath;
        _enginePath = enginePath;
        _uprojectPath = uprojectPath;
        LoadFromConfig();
    }

    /// <summary>
    /// Load current config values into the VM properties.
    /// </summary>
    private void LoadFromConfig()
    {
        var config = ConfigService.LoadConfig(_repoPath);
        var localState = ConfigService.LoadLocalState(_repoPath);

        // Engine (local)
        EnginePathOverride = localState.EnginePathOverride;
        AutoDetectEngine = config.Engine?.AutoDetect ?? true;

        // Network (shared)
        NetworkBaseUrl = config.NetworkBase;
        ArchiveChannel = config.Archive?.Channel ?? "Editor";
        PublishChannel = config.Publish?.Channel ?? "Editor";
        CustomArchiveChannel = config.Archive?.CustomChannel ?? string.Empty;
        CustomPublishChannel = config.Publish?.CustomChannel ?? string.Empty;

        // Build defaults (shared)
        DefaultBuildConfig = config.BuildDefaults?.DefaultConfig ?? "Development";
        BuildContentWhenPackaging = config.BuildDefaults?.BuildContentWhenPackaging ?? false;
        OutputDirectory = config.BuildDefaults?.OutputDirectory ?? "Saved/StagedBuilds";

        // Publish (shared)
        AtomicPublish = config.Publish?.Atomic ?? true;

        // Build targets (shared) — already migrated by ConfigService
        BuildTargets.Clear();
        foreach (var step in config.Engine?.BuildTargets ?? new())
        {
            BuildTargets.Add(BuildTargetEditModel.FromStep(step));
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (!Validate()) return;

        var sharedConfig = ConfigService.LoadConfig(_repoPath);
        var localState = ConfigService.LoadLocalState(_repoPath);

        // Update shared config — use with expressions since config types are now immutable records (fixes M-2)
        // Bump version to 2 since we now persist BuildMode/UatCommand (Council F-13)
        sharedConfig = sharedConfig with
        {
            Version = 2,
            NetworkBase = NetworkBaseUrl,
            Engine = sharedConfig.Engine with
            {
                BuildTargets = new List<UgsBuildStep>(BuildTargets.Select(x => x.ToStep())),
                AutoDetect = AutoDetectEngine
            }
        };

        if (sharedConfig.Archive != null)
        {
            sharedConfig = sharedConfig with
            {
                Archive = sharedConfig.Archive with
                {
                    Channel = ArchiveChannel,
                    CustomChannel = CustomArchiveChannel,
                }
            };
        }

        if (sharedConfig.BuildDefaults != null)
        {
            sharedConfig = sharedConfig with
            {
                BuildDefaults = sharedConfig.BuildDefaults with
                {
                    DefaultConfig = DefaultBuildConfig,
                    BuildContentWhenPackaging = BuildContentWhenPackaging,
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
                    Channel = PublishChannel,
                    CustomChannel = CustomPublishChannel,
                    Atomic = AtomicPublish,
                }
            };
        }

        ConfigService.SaveConfig(_repoPath, sharedConfig);

        // Update local config (fixes D-1: engine path is user-local)
        localState.EnginePathOverride = EnginePathOverride;
        ConfigService.SaveLocalState(_repoPath, localState);
    }

    [RelayCommand]
    private void Cancel()
    {
        // Dialog close handled by view
    }

    [RelayCommand]
    private void AddBuildTarget()
    {
        var newTarget = new BuildTargetEditModel();
        newTarget.ApplyDefaults(resetDirty: true);  // Pre-populate defaults for UBT mode
        BuildTargets.Add(newTarget);
    }

    [RelayCommand]
    private void RemoveBuildTarget(BuildTargetEditModel step)
    {
        BuildTargets.Remove(step);
    }

    /// <summary>
    /// Validate settings before saving (fixes H-4).
    /// </summary>
    private bool Validate()
    {
        var valid = true;
        EnginePathError = string.Empty;
        NetworkUrlError = string.Empty;

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

        return valid;
    }

    /// <summary>
    /// Converts between BuildMode string ("Ubt"/"Uat"/"Custom") and ComboBox SelectedIndex (0/1/2).
    /// </summary>
    public class BuildModeConverter : Avalonia.Data.Converters.IValueConverter
    {
        public static readonly BuildModeConverter Instance = new();

        private static readonly string[] Modes = { BuildModes.Ubt, BuildModes.Uat, BuildModes.Custom };

        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string mode)
            {
                var idx = System.Array.IndexOf(Modes, mode);
                return idx >= 0 ? idx : 0;
            }
            return 0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int idx && idx >= 0 && idx < Modes.Length)
                return Modes[idx];
            return BuildModes.Ubt;
        }
    }

    /// <summary>
    /// Provides context-aware watermark text for the ScriptPath TextBox based on BuildMode.
    /// </summary>
    public class BuildModeScriptPathHintConverter : Avalonia.Data.Converters.IValueConverter
    {
        public static readonly BuildModeScriptPathHintConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string mode)
            {
                return mode switch
                {
                    BuildModes.Ubt => "e.g. {EnginePath}/Engine/Build/BatchFiles/Build.bat",
                    BuildModes.Uat => "e.g. {EnginePath}/Engine/Build/BatchFiles/RunUAT.bat",
                    BuildModes.Custom => "Path to your custom build script",
                    _ => "Script path"
                };
            }
            return "Script path";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            // One-way converter
            return null;
        }
    }
}