using CommunityToolkit.Mvvm.ComponentModel;

using UGSGit.PluginAbstractions;

namespace UGSGit.Plugins.UnrealSync.ViewModels;

/// <summary>
/// ViewModel shown when the UE engine cannot be detected for the project.
/// </summary>
public class EngineNotFoundViewModel : ObservableObject
{
    private readonly UProjectMeta _meta;

    /// <summary>
    /// Dialog title.
    /// </summary>
    public string Title => "Engine Not Found";

    /// <summary>
    /// Project name from .uproject metadata.
    /// </summary>
    public string ProjectName => _meta.EngineAssociation ?? "Unknown";

    /// <summary>
    /// Engine identifier from .uproject file.
    /// </summary>
    public string EngineAssociation => _meta.EngineAssociation ?? "Not set";

    /// <summary>
    /// User-facing error message explaining why the engine was not found.
    /// </summary>
    public string Message => "Engine path could not be determined automatically.";

    /// <summary>
    /// List of common causes and troubleshooting steps.
    /// </summary>
    public string[] PossibleReasons => new[]
    {
        "No matching registry entry for this engine GUID",
        "Not a relative path to '../UE5' or '../UnrealEngine'",
        "UNREAL_ENGINE_PATH environment variable not set",
        "No .unrealsync.json with engine.path configured"
    };

    /// <summary>
    /// Initializes a new instance of <see cref="EngineNotFoundViewModel"/>.
    /// </summary>
    /// <param name="meta">Parsed .uproject metadata used to populate engine association info.</param>
    public EngineNotFoundViewModel(UProjectMeta meta)
    {
        _meta = meta;
    }
}
