using System;
using System.IO;
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

    // Validation errors (fixes H-4)
    [ObservableProperty] private string _enginePathError = string.Empty;
    [ObservableProperty] private string _networkUrlError = string.Empty;

    public SettingsDialogViewModel(string repoPath)
    {
        _repoPath = repoPath;
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
    }

    [RelayCommand]
    private void Save()
    {
        if (!Validate()) return;

        var sharedConfig = ConfigService.LoadConfig(_repoPath);
        var localState = ConfigService.LoadLocalState(_repoPath);

        // Update shared config — mutate properties directly since these are class types, not records
        sharedConfig.NetworkBase = NetworkBaseUrl;

        if (sharedConfig.Archive != null)
        {
            sharedConfig.Archive.Channel = ArchiveChannel;
            sharedConfig.Archive.CustomChannel = CustomArchiveChannel;
        }

        if (sharedConfig.BuildDefaults != null)
        {
            sharedConfig.BuildDefaults.DefaultConfig = DefaultBuildConfig;
            sharedConfig.BuildDefaults.BuildContentWhenPackaging = BuildContentWhenPackaging;
            sharedConfig.BuildDefaults.OutputDirectory = OutputDirectory;
        }

        if (sharedConfig.Publish != null)
        {
            sharedConfig.Publish.Channel = PublishChannel;
            sharedConfig.Publish.CustomChannel = CustomPublishChannel;
            sharedConfig.Publish.Atomic = AtomicPublish;
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
}