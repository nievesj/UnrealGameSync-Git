using CommunityToolkit.Mvvm.ComponentModel;

using SourceGit.Models;

namespace SourceGit.ViewModels.Tabs.UnrealSync;

public class EngineNotFoundViewModel : ObservableObject
{
    private readonly UProjectMeta _meta;

    public string Title => "Engine Not Found";
    public string ProjectName => _meta.EngineAssociation ?? "Unknown";
    public string EngineAssociation => _meta.EngineAssociation ?? "Not set";
    public string Message => "Engine path could not be determined automatically.";

    public string[] PossibleReasons => new[]
    {
        "No matching registry entry for this engine GUID",
        "Not a relative path to '../UE5' or '../UnrealEngine'",
        "UNREAL_ENGINE_PATH environment variable not set",
        "No .unrealsync.json with engine.path configured"
    };

    public EngineNotFoundViewModel(UProjectMeta meta)
    {
        _meta = meta;
    }
}
